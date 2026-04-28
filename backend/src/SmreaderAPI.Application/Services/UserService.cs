using Microsoft.Extensions.Logging;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Mappings;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IAuthService authService,
        ICacheService cacheService,
        IAuditService auditService,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
        _cacheService = cacheService;
        _auditService = auditService;
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
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
        if (user is null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid email or password.");

        if (!user.IsActive)
            return ApiResponse<TokenResponseDto>.FailResponse("Account is deactivated.");

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
        var roleName = role?.Name ?? "User";

        var accessToken = _authService.GenerateJwtToken(user, roleName);
        var refreshTokenValue = _authService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);

        await _auditService.LogActionAsync(user.Id, "Login", "User", user.Id);

        _logger.LogInformation("User logged in: {Email}", dto.Email);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
            "Login successful.");
    }

    public async Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var existingToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(dto.RefreshToken);
        if (existingToken is null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow)
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid or expired refresh token.");

        await _unitOfWork.RefreshTokens.RevokeTokenAsync(existingToken.Id);

        var user = await _unitOfWork.Users.GetByIdAsync(existingToken.UserId);
        if (user is null)
            return ApiResponse<TokenResponseDto>.FailResponse("User not found.");

        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
        var roleName = role?.Name ?? "User";

        var accessToken = _authService.GenerateJwtToken(user, roleName);
        var newRefreshTokenValue = _authService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, newRefreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
            "Token refreshed.");
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
