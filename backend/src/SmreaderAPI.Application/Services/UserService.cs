using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Mappings;
using SmreaderAPI.Application.Tenancy;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Services;

public class UserService : IUserService
{
    private readonly ITenantUnitOfWorkFactory _unitOfWorkFactory;
    private readonly ITenantConnectionResolver _tenantConnectionResolver;
    private readonly ITenantDatabaseRepository _tenantDatabaseRepository;
    private readonly IMasterRefreshTokenRepository _masterRefreshTokenRepository;
    private readonly TenantContext _tenantContext;
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserService> _logger;
    private readonly IConfiguration _configuration;

    public UserService(
        ITenantUnitOfWorkFactory unitOfWorkFactory,
        ITenantConnectionResolver tenantConnectionResolver,
        ITenantDatabaseRepository tenantDatabaseRepository,
        IMasterRefreshTokenRepository masterRefreshTokenRepository,
        TenantContext tenantContext,
        IAuthService authService,
        ICacheService cacheService,
        IAuditService auditService,
        ILogger<UserService> logger,
        IConfiguration configuration)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _tenantConnectionResolver = tenantConnectionResolver;
        _tenantDatabaseRepository = tenantDatabaseRepository;
        _masterRefreshTokenRepository = masterRefreshTokenRepository;
        _tenantContext = tenantContext;
        _authService = authService;
        _cacheService = cacheService;
        _auditService = auditService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterDto dto)
    {
        (string FinancialYear, string ConnectionString) tenant;
        try
        {
            tenant = await ResolveTenantAsync(dto.TenantId, dto.FinancialYear);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<TokenResponseDto>.FailResponse(ex.Message);
        }

        var (financialYear, connectionString) = tenant;
        _tenantContext.Set(dto.TenantId, financialYear, connectionString);

        using var unitOfWork = _unitOfWorkFactory.Create(connectionString);
        var existingUser = await unitOfWork.Users.GetByEmailAsync(dto.Email);
        if (existingUser is not null)
            return ApiResponse<TokenResponseDto>.FailResponse("Email already registered.");

        var defaultRole = await unitOfWork.Roles.GetByNameAsync("User");
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

        await unitOfWork.BeginTransactionAsync();
        try
        {
            user.Id = await unitOfWork.Users.AddAsync(user);

            var accessToken = _authService.GenerateJwtToken(user, defaultRole.Name, dto.TenantId, financialYear);
            var refreshTokenValue = _authService.GenerateRefreshToken();
            var refreshTokenHash = _authService.HashRefreshToken(refreshTokenValue);

            var refreshToken = new MasterRefreshToken
            {
                UserId = user.Id,
                TenantId = dto.TenantId,
                FinancialYear = financialYear,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
                CreatedAt = DateTime.UtcNow
            };
            await _masterRefreshTokenRepository.AddAsync(refreshToken);

            await unitOfWork.CommitAsync();

            await _auditService.LogActionAsync(user.Id, "Register", "User", user.Id);

            _logger.LogInformation("User registered: {Email}", dto.Email);

            return ApiResponse<TokenResponseDto>.SuccessResponse(
                new TokenResponseDto(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())),
                "Registration successful.");
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto)
    {
        (string FinancialYear, string ConnectionString) tenant;
        try
        {
            tenant = await ResolveTenantAsync(dto.TenantId, dto.FinancialYear);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<TokenResponseDto>.FailResponse(ex.Message);
        }

        var (financialYear, connectionString) = tenant;
        _tenantContext.Set(dto.TenantId, financialYear, connectionString);

        using var unitOfWork = _unitOfWorkFactory.Create(connectionString);
        var user = await unitOfWork.Users.GetByEmailAsync(dto.Email);
        if (user is null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid email or password.");

        if (!user.IsActive)
            return ApiResponse<TokenResponseDto>.FailResponse("Account is deactivated.");

        var role = await unitOfWork.Roles.GetByIdAsync(user.RoleId);
        var roleName = role?.Name ?? "User";

        var accessToken = _authService.GenerateJwtToken(user, roleName, dto.TenantId, financialYear);
        var refreshTokenValue = _authService.GenerateRefreshToken();
        var refreshTokenHash = _authService.HashRefreshToken(refreshTokenValue);

        var refreshToken = new MasterRefreshToken
        {
            UserId = user.Id,
            TenantId = dto.TenantId,
            FinancialYear = financialYear,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            CreatedAt = DateTime.UtcNow
        };
        await _masterRefreshTokenRepository.AddAsync(refreshToken);

        await _auditService.LogActionAsync(user.Id, "Login", "User", user.Id);

        _logger.LogInformation("User logged in: {Email}", dto.Email);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())),
            "Login successful.");
    }

    public async Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var tokenHash = _authService.HashRefreshToken(dto.RefreshToken);
        var existingToken = await _masterRefreshTokenRepository.GetByTokenHashAsync(tokenHash);
        if (existingToken is null || existingToken.RevokedAt is not null || existingToken.ExpiresAt <= DateTime.UtcNow)
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid or expired refresh token.");

        string connectionString;
        try
        {
            connectionString = await _tenantConnectionResolver.ResolveConnectionStringAsync(
                existingToken.TenantId,
                existingToken.FinancialYear);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<TokenResponseDto>.FailResponse(ex.Message);
        }
        _tenantContext.Set(existingToken.TenantId, existingToken.FinancialYear, connectionString);

        using var unitOfWork = _unitOfWorkFactory.Create(connectionString);
        var user = await unitOfWork.Users.GetByIdAsync(existingToken.UserId);
        if (user is null || !user.IsActive)
            return ApiResponse<TokenResponseDto>.FailResponse("User not found or inactive.");

        var role = await unitOfWork.Roles.GetByIdAsync(user.RoleId);
        var roleName = role?.Name ?? "User";

        var accessToken = _authService.GenerateJwtToken(
            user,
            roleName,
            existingToken.TenantId,
            existingToken.FinancialYear);
        var newRefreshTokenValue = _authService.GenerateRefreshToken();
        var newRefreshTokenHash = _authService.HashRefreshToken(newRefreshTokenValue);

        await _masterRefreshTokenRepository.RevokeAsync(existingToken.Id, newRefreshTokenHash);

        var newRefreshToken = new MasterRefreshToken
        {
            UserId = user.Id,
            TenantId = existingToken.TenantId,
            FinancialYear = existingToken.FinancialYear,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            CreatedAt = DateTime.UtcNow
        };
        await _masterRefreshTokenRepository.AddAsync(newRefreshToken);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, newRefreshTokenValue, DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())),
            "Token refreshed.");
    }

    public async Task<ApiResponse<TokenResponseDto>> SwitchFinancialYearAsync(int userId, int tenantId, SwitchFinancialYearDto dto)
    {
        if (!await _tenantDatabaseRepository.FinancialYearExistsAsync(tenantId, dto.FinancialYear))
            return ApiResponse<TokenResponseDto>.FailResponse("Financial year not found for tenant.");

        string connectionString;
        try
        {
            connectionString = await _tenantConnectionResolver.ResolveConnectionStringAsync(tenantId, dto.FinancialYear);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<TokenResponseDto>.FailResponse(ex.Message);
        }
        _tenantContext.Set(tenantId, dto.FinancialYear, connectionString);

        using var unitOfWork = _unitOfWorkFactory.Create(connectionString);
        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<TokenResponseDto>.FailResponse("User not found in target financial year.");

        var role = await unitOfWork.Roles.GetByIdAsync(user.RoleId);
        var roleName = role?.Name ?? "User";
        var accessToken = _authService.GenerateJwtToken(user, roleName, tenantId, dto.FinancialYear);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, string.Empty, DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes())),
            "Financial year switched.");
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var cacheKey = GetUserCacheKey(id);
        var cached = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cached is not null)
            return ApiResponse<UserDto>.SuccessResponse(cached);

        using var unitOfWork = _unitOfWorkFactory.Create(GetTenantConnectionStringOrThrow());
        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        var role = await unitOfWork.Roles.GetByIdAsync(user.RoleId);
        var dto = user.ToDto(role?.Name ?? "User");

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));

        return ApiResponse<UserDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync()
    {
        using var unitOfWork = _unitOfWorkFactory.Create(GetTenantConnectionStringOrThrow());
        var users = await unitOfWork.Users.GetAllAsync();
        var roles = (await unitOfWork.Roles.GetAllAsync()).ToDictionary(r => r.Id, r => r.Name);

        var dtos = users.Select(u => u.ToDto(roles.GetValueOrDefault(u.RoleId, "User")));
        return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto)
    {
        using var unitOfWork = _unitOfWorkFactory.Create(GetTenantConnectionStringOrThrow());
        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        if (dto.Name is not null) user.Name = dto.Name;
        if (dto.Email is not null) user.Email = dto.Email;
        if (dto.RoleId.HasValue) user.RoleId = dto.RoleId.Value;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Users.UpdateAsync(user);
            await unitOfWork.CommitAsync();

            await _cacheService.RemoveAsync(GetUserCacheKey(id));
            await _auditService.LogActionAsync(null, "Update", "User", id, $"Updated fields: {string.Join(", ", GetUpdatedFields(dto))}");

            var role = await unitOfWork.Roles.GetByIdAsync(user.RoleId);
            return ApiResponse<UserDto>.SuccessResponse(user.ToDto(role?.Name ?? "User"), "User updated.");
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        using var unitOfWork = _unitOfWorkFactory.Create(GetTenantConnectionStringOrThrow());
        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<bool>.FailResponse("User not found.");

        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Users.DeleteAsync(id);
            await unitOfWork.CommitAsync();

            await _cacheService.RemoveAsync(GetUserCacheKey(id));
            await _auditService.LogActionAsync(null, "Delete", "User", id);

            return ApiResponse<bool>.SuccessResponse(true, "User deleted.");
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<(string FinancialYear, string ConnectionString)> ResolveTenantAsync(int tenantId, string? financialYear)
    {
        if (string.IsNullOrWhiteSpace(financialYear))
        {
            var defaultDb = await _tenantDatabaseRepository.GetDefaultAsync(tenantId);
            if (defaultDb is null)
                throw new InvalidOperationException("Default financial year not configured for tenant.");

            return (defaultDb.FinancialYear, defaultDb.ConnectionString);
        }

        var connectionString = await _tenantConnectionResolver.ResolveConnectionStringAsync(tenantId, financialYear);
        return (financialYear, connectionString);
    }

    private string GetTenantConnectionStringOrThrow()
    {
        if (string.IsNullOrWhiteSpace(_tenantContext.ConnectionString))
            throw new InvalidOperationException("Tenant connection string is not set for this request.");

        return _tenantContext.ConnectionString;
    }

    private string GetUserCacheKey(int userId)
    {
        var tenantId = _tenantContext.TenantId;
        return tenantId.HasValue ? $"tenant:{tenantId.Value}:user:{userId}" : $"user:{userId}";
    }

    private int GetRefreshTokenExpiryDays()
        => int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");

    private int GetAccessTokenExpiryMinutes()
        => int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "30");

    private static IEnumerable<string> GetUpdatedFields(UpdateUserDto dto)
    {
        if (dto.Name is not null) yield return "Name";
        if (dto.Email is not null) yield return "Email";
        if (dto.RoleId.HasValue) yield return "RoleId";
        if (dto.IsActive.HasValue) yield return "IsActive";
    }
}
