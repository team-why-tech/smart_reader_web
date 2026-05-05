using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Mappings;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Entities.Master;
using SmreaderAPI.Domain.Interfaces;
using System.Security.Claims;

namespace SmreaderAPI.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMasterUnitOfWork _masterUnitOfWork;
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly ITenantConnectionResolver _tenantConnectionResolver;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IMasterUnitOfWork masterUnitOfWork,
        IAuthService authService,
        ICacheService cacheService,
        IAuditService auditService,
        ITenantConnectionResolver tenantConnectionResolver,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _masterUnitOfWork = masterUnitOfWork;
        _authService = authService;
        _cacheService = cacheService;
        _auditService = auditService;
        _tenantConnectionResolver = tenantConnectionResolver;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
        if (existingUser is not null)
            return ApiResponse<TokenResponseDto>.FailResponse("Email already registered.");

        var defaultRole = await _unitOfWork.Roles.GetByNameAsync("User");
        if (defaultRole is null)
            return ApiResponse<TokenResponseDto>.FailResponse("Default role not found.");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = _authService.HashPassword(dto.Password),
            RoleId = defaultRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.Id = await _unitOfWork.Users.AddAsync(user);

            var accessToken = _authService.GenerateJwtToken(user, defaultRole.Name);
            var refreshTokenValue = _authService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);

            await _unitOfWork.CommitAsync();

            await _auditService.LogActionAsync(user.Id, "Register", "User", user.Id);

            _logger.LogInformation("User registered: {Email}", dto.Email);

            return ApiResponse<TokenResponseDto>.SuccessResponse(
                new TokenResponseDto(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
                "Registration successful.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto)
    {
        // 1. Look up tenant by code in master DB
        var tenant = await _masterUnitOfWork.Tenants.GetByCodeAsync(dto.TenantCode);
        if (tenant is null || !tenant.IsActive)
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid tenant code or tenant is inactive.");

        // 2. Get default FY connection string for tenant
        var tenantDb = await _masterUnitOfWork.TenantDatabases.GetDefaultForTenantAsync(tenant.Id);
        if (tenantDb is null)
            return ApiResponse<TokenResponseDto>.FailResponse("No default database configured for tenant.");

        // 3. Connect to tenant DB and validate credentials
        User? user = null;
        Role? role = null;

        var connectionString = tenantDb.ConnectionString;
        await using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            // Query user
            var userSql = "SELECT * FROM Users WHERE Email = @Email LIMIT 1";
            await using (var cmd = new MySqlCommand(userSql, connection))
            {
                cmd.Parameters.AddWithValue("@Email", dto.Email);
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = new User
                        {
                            Id = reader.GetInt32("Id"),
                            Name = reader.GetString("Name"),
                            Email = reader.GetString("Email"),
                            PasswordHash = reader.GetString("PasswordHash"),
                            RoleId = reader.GetInt32("RoleId"),
                            IsActive = reader.GetBoolean("IsActive"),
                            CreatedAt = reader.GetDateTime("CreatedAt")
                        };
                    }
                }
            }

            if (user is null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
                return ApiResponse<TokenResponseDto>.FailResponse("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponse<TokenResponseDto>.FailResponse("Account is deactivated.");

            // Query role
            var roleSql = "SELECT * FROM Roles WHERE Id = @RoleId LIMIT 1";
            await using (var roleCmd = new MySqlCommand(roleSql, connection))
            {
                roleCmd.Parameters.AddWithValue("@RoleId", user.RoleId);
                await using (var roleReader = await roleCmd.ExecuteReaderAsync())
                {
                    if (await roleReader.ReadAsync())
                    {
                        role = new Role
                        {
                            Id = roleReader.GetInt32("Id"),
                            Name = roleReader.GetString("Name")
                        };
                    }
                }
            }
        }

        var roleName = role?.Name ?? "User";

        // 4. Generate JWT with tenant_id + fy claims
        var accessToken = _authService.GenerateJwtToken(user, roleName, tenant.Id, tenantDb.FinancialYear);
        var refreshTokenValue = _authService.GenerateRefreshToken();

        // 5. Store refresh token in master DB
        var refreshToken = new MasterRefreshToken
        {
            UserId = user.Id,
            TenantId = tenant.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _masterUnitOfWork.RefreshTokens.AddAsync(refreshToken);

        _logger.LogInformation("User logged in: {Email}, Tenant: {TenantCode}, FY: {FY}", 
            dto.Email, dto.TenantCode, tenantDb.FinancialYear);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
            "Login successful.");
    }

    public async Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        // 1. Validate refresh token in master DB
        var existingToken = await _masterUnitOfWork.RefreshTokens.GetByTokenAsync(dto.RefreshToken);
        if (existingToken is null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow)
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid or expired refresh token.");

        // 2. Get tenant and default FY info from master DB
        var tenant = await _masterUnitOfWork.Tenants.GetByIdAsync(existingToken.TenantId);
        if (tenant is null || !tenant.IsActive)
            return ApiResponse<TokenResponseDto>.FailResponse("Tenant not found or inactive.");

        var tenantDb = await _masterUnitOfWork.TenantDatabases.GetDefaultForTenantAsync(tenant.Id);
        if (tenantDb is null)
            return ApiResponse<TokenResponseDto>.FailResponse("No default database configured for tenant.");

        // 3. Get user from tenant DB
        User? user = null;
        Role? role = null;

        var connectionString = tenantDb.ConnectionString;
        await using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var userSql = "SELECT * FROM Users WHERE Id = @UserId LIMIT 1";
            await using (var cmd = new MySqlCommand(userSql, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", existingToken.UserId);
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = new User
                        {
                            Id = reader.GetInt32("Id"),
                            Name = reader.GetString("Name"),
                            Email = reader.GetString("Email"),
                            PasswordHash = reader.GetString("PasswordHash"),
                            RoleId = reader.GetInt32("RoleId"),
                            IsActive = reader.GetBoolean("IsActive")
                        };
                    }
                }
            }

            if (user is null)
                return ApiResponse<TokenResponseDto>.FailResponse("User not found.");

            if (!user.IsActive)
                return ApiResponse<TokenResponseDto>.FailResponse("Account is deactivated.");

            // Query role
            var roleSql = "SELECT * FROM Roles WHERE Id = @RoleId LIMIT 1";
            await using (var roleCmd = new MySqlCommand(roleSql, connection))
            {
                roleCmd.Parameters.AddWithValue("@RoleId", user.RoleId);
                await using (var roleReader = await roleCmd.ExecuteReaderAsync())
                {
                    if (await roleReader.ReadAsync())
                    {
                        role = new Role
                        {
                            Id = roleReader.GetInt32("Id"),
                            Name = roleReader.GetString("Name")
                        };
                    }
                }
            }
        }

        var roleName = role?.Name ?? "User";

        // 4. Revoke old refresh token
        var newRefreshTokenValue = _authService.GenerateRefreshToken();
        await _masterUnitOfWork.RefreshTokens.RevokeTokenAsync(existingToken.Id, newRefreshTokenValue);

        // 5. Generate new tokens
        var accessToken = _authService.GenerateJwtToken(user, roleName, tenant.Id, tenantDb.FinancialYear);

        var newRefreshToken = new MasterRefreshToken
        {
            UserId = user.Id,
            TenantId = tenant.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _masterUnitOfWork.RefreshTokens.AddAsync(newRefreshToken);

        _logger.LogInformation("Token refreshed for user: {UserId}, Tenant: {TenantId}", user.Id, tenant.Id);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, newRefreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
            "Token refreshed.");
    }

    public async Task<ApiResponse<TokenResponseDto>> SwitchFinancialYearAsync(SwitchFyDto dto)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated ?? true)
            return ApiResponse<TokenResponseDto>.FailResponse("User not authenticated.");

        // Extract current tenant_id and user_id from JWT
        var tenantIdClaim = user.FindFirst("tenant_id")?.Value;
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userIdClaim))
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid token: missing required claims.");

        if (!int.TryParse(tenantIdClaim, out var tenantId) || !int.TryParse(userIdClaim, out var userId))
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid token: malformed claims.");

        // Validate that the requested FY exists for this tenant
        var tenantDb = await _masterUnitOfWork.TenantDatabases.GetByTenantAndFyAsync(tenantId, dto.FinancialYear);
        if (tenantDb is null)
            return ApiResponse<TokenResponseDto>.FailResponse($"Financial year '{dto.FinancialYear}' not found for your tenant.");

        // Get user from the requested FY database
        User? targetUser = null;
        Role? role = null;

        var connectionString = tenantDb.ConnectionString;
        await using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var userSql = "SELECT * FROM Users WHERE Id = @UserId LIMIT 1";
            await using (var cmd = new MySqlCommand(userSql, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        targetUser = new User
                        {
                            Id = reader.GetInt32("Id"),
                            Name = reader.GetString("Name"),
                            Email = reader.GetString("Email"),
                            PasswordHash = reader.GetString("PasswordHash"),
                            RoleId = reader.GetInt32("RoleId"),
                            IsActive = reader.GetBoolean("IsActive")
                        };
                    }
                }
            }

            if (targetUser is null)
                return ApiResponse<TokenResponseDto>.FailResponse("User not found in the requested financial year database.");

            if (!targetUser.IsActive)
                return ApiResponse<TokenResponseDto>.FailResponse("Account is deactivated in the requested financial year.");

            // Query role
            var roleSql = "SELECT * FROM Roles WHERE Id = @RoleId LIMIT 1";
            await using (var roleCmd = new MySqlCommand(roleSql, connection))
            {
                roleCmd.Parameters.AddWithValue("@RoleId", targetUser.RoleId);
                await using (var roleReader = await roleCmd.ExecuteReaderAsync())
                {
                    if (await roleReader.ReadAsync())
                    {
                        role = new Role
                        {
                            Id = roleReader.GetInt32("Id"),
                            Name = roleReader.GetString("Name")
                        };
                    }
                }
            }
        }

        var roleName = role?.Name ?? "User";

        // Generate new JWT with updated FY claim
        var accessToken = _authService.GenerateJwtToken(targetUser, roleName, tenantId, dto.FinancialYear);
        var refreshTokenValue = _authService.GenerateRefreshToken();

        // Store new refresh token in master DB
        var refreshToken = new MasterRefreshToken
        {
            UserId = targetUser.Id,
            TenantId = tenantId,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _masterUnitOfWork.RefreshTokens.AddAsync(refreshToken);

        _logger.LogInformation("User switched FY: UserId={UserId}, TenantId={TenantId}, NewFY={FY}", 
            userId, tenantId, dto.FinancialYear);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
            $"Switched to financial year {dto.FinancialYear}.");
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var cacheKey = $"user:{id}";
        var cached = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cached is not null)
            return ApiResponse<UserDto>.SuccessResponse(cached);

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
        var dto = user.ToDto(role?.Name ?? "User");

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));

        return ApiResponse<UserDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var roles = (await _unitOfWork.Roles.GetAllAsync()).ToDictionary(r => r.Id, r => r.Name);

        var dtos = users.Select(u => u.ToDto(roles.GetValueOrDefault(u.RoleId, "User")));
        return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        if (dto.Name is not null) user.Name = dto.Name;
        if (dto.Email is not null) user.Email = dto.Email;
        if (dto.RoleId.HasValue) user.RoleId = dto.RoleId.Value;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            await _cacheService.RemoveAsync($"user:{id}");
            await _auditService.LogActionAsync(null, "Update", "User", id, $"Updated fields: {string.Join(", ", GetUpdatedFields(dto))}");

            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
            return ApiResponse<UserDto>.SuccessResponse(user.ToDto(role?.Name ?? "User"), "User updated.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<bool>.FailResponse("User not found.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Users.DeleteAsync(id);
            await _unitOfWork.CommitAsync();

            await _cacheService.RemoveAsync($"user:{id}");
            await _auditService.LogActionAsync(null, "Delete", "User", id);

            return ApiResponse<bool>.SuccessResponse(true, "User deleted.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static IEnumerable<string> GetUpdatedFields(UpdateUserDto dto)
    {
        if (dto.Name is not null) yield return "Name";
        if (dto.Email is not null) yield return "Email";
        if (dto.RoleId.HasValue) yield return "RoleId";
        if (dto.IsActive.HasValue) yield return "IsActive";
    }
}
