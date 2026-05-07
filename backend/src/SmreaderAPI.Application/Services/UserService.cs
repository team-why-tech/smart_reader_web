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
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IAuthService authService,
        IRefreshTokenRepository refreshTokenRepo,
        ICacheService cacheService,
        IAuditService auditService,
        ITenantContext tenantContext,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
        _refreshTokenRepo = refreshTokenRepo;
        _cacheService = cacheService;
        _auditService = auditService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<ApiResponse<TokenResponseDto>> LoginAsync(TenantLoginDto dto)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
        if (user is null || !_authService.VerifyLegacyPassword(dto.Password, user.Pwd))
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid email or password.");

        if (user.Status == 0)
            return ApiResponse<TokenResponseDto>.FailResponse("Account is deactivated.");

        var accessToken = _authService.GenerateJwtToken(user, dto.TenantId);
        var refreshTokenValue = _authService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            TenantId = dto.TenantId,
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepo.AddAsync(refreshToken);

        //await _auditService.LogActionAsync(user.Id, "Login", "User", user.Id);

        _logger.LogInformation("User logged in: {Email} (Tenant: {TenantId})", dto.Email, dto.TenantId);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, refreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
            "Login successful.");
    }

    public async Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var existingToken = await _refreshTokenRepo.GetByTokenAsync(dto.RefreshToken);
        if (existingToken is null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow)
            return ApiResponse<TokenResponseDto>.FailResponse("Invalid or expired refresh token.");

        await _refreshTokenRepo.RevokeTokenAsync(existingToken.Id);

        var user = await _unitOfWork.Users.GetByIdAsync(existingToken.UserId);
        if (user is null)
            return ApiResponse<TokenResponseDto>.FailResponse("User not found.");

        var accessToken = _authService.GenerateJwtToken(user, _tenantContext.TenantId);
        var newRefreshTokenValue = _authService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            TenantId = _tenantContext.TenantId,
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepo.AddAsync(newRefreshToken);

        return ApiResponse<TokenResponseDto>.SuccessResponse(
            new TokenResponseDto(accessToken, newRefreshTokenValue, DateTime.UtcNow.AddMinutes(30)),
            "Token refreshed.");
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var cacheKey = $"tenant:{_tenantContext.TenantId}:user:{id}";
        var cached = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cached is not null)
            return ApiResponse<UserDto>.SuccessResponse(cached);

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        var dto = user.ToDto();

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));

        return ApiResponse<UserDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var dtos = users.Select(u => u.ToDto());
        return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.FailResponse("User not found.");

        if (dto.Name is not null) user.Name = dto.Name;
        if (dto.Email is not null) user.Email = dto.Email;
        if (dto.Mobile is not null) user.Mobile = dto.Mobile;
        if (dto.Status.HasValue) user.Status = dto.Status.Value;
        if (dto.Address is not null) user.Address = dto.Address;
        if (dto.OwnerGuid.HasValue) user.OwnerGuid = dto.OwnerGuid.Value;
        if (dto.Privilages is not null) user.Privilages = dto.Privilages;
        if (dto.CategoryGuid.HasValue) user.CategoryGuid = dto.CategoryGuid.Value;
        if (dto.VanSale.HasValue) user.VanSale = dto.VanSale.Value;
        if (dto.Tech.HasValue) user.Tech = dto.Tech.Value;
        if (dto.UserInactive.HasValue) user.UserInactive = dto.UserInactive.Value;
        if (dto.CollectionAgent.HasValue) user.CollectionAgent = dto.CollectionAgent.Value;
        if (dto.SuperAdmin.HasValue) user.SuperAdmin = dto.SuperAdmin.Value;
        if (dto.Printertype.HasValue) user.Printertype = dto.Printertype.Value;
        if (dto.Moduletype.HasValue) user.Moduletype = dto.Moduletype.Value;
        if (dto.Billnumber.HasValue) user.Billnumber = dto.Billnumber.Value;
        if (dto.ReadBillnumber.HasValue) user.ReadBillnumber = dto.ReadBillnumber.Value;
        if (dto.Panchayatname.HasValue) user.Panchayatname = dto.Panchayatname.Value;
        if (dto.Panchayatname1.HasValue) user.Panchayatname1 = dto.Panchayatname1.Value;
        if (dto.Panchayatname2.HasValue) user.Panchayatname2 = dto.Panchayatname2.Value;
        if (dto.Panchayatname3.HasValue) user.Panchayatname3 = dto.Panchayatname3.Value;
        if (dto.Panchayatname4.HasValue) user.Panchayatname4 = dto.Panchayatname4.Value;
        if (dto.EmailCRM is not null) user.EmailCRM = dto.EmailCRM;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            var cacheKey = $"tenant:{_tenantContext.TenantId}:user:{id}";
            await _cacheService.RemoveAsync(cacheKey);
            await _auditService.LogActionAsync(null, "Update", "User", id, $"Updated fields: {string.Join(", ", GetUpdatedFields(dto))}");

            return ApiResponse<UserDto>.SuccessResponse(user.ToDto(), "User updated.");
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

            var cacheKey = $"tenant:{_tenantContext.TenantId}:user:{id}";
            await _cacheService.RemoveAsync(cacheKey);
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
        if (dto.Mobile is not null) yield return "Mobile";
        if (dto.Status.HasValue) yield return "Status";
        if (dto.Address is not null) yield return "Address";
        if (dto.OwnerGuid.HasValue) yield return "OwnerGuid";
        if (dto.Privilages is not null) yield return "Privilages";
        if (dto.CategoryGuid.HasValue) yield return "CategoryGuid";
        if (dto.VanSale.HasValue) yield return "VanSale";
        if (dto.Tech.HasValue) yield return "Tech";
        if (dto.UserInactive.HasValue) yield return "UserInactive";
        if (dto.CollectionAgent.HasValue) yield return "CollectionAgent";
        if (dto.SuperAdmin.HasValue) yield return "SuperAdmin";
        if (dto.Printertype.HasValue) yield return "Printertype";
        if (dto.Moduletype.HasValue) yield return "Moduletype";
        if (dto.Billnumber.HasValue) yield return "Billnumber";
        if (dto.ReadBillnumber.HasValue) yield return "ReadBillnumber";
        if (dto.Panchayatname.HasValue) yield return "Panchayatname";
        if (dto.Panchayatname1.HasValue) yield return "Panchayatname1";
        if (dto.Panchayatname2.HasValue) yield return "Panchayatname2";
        if (dto.Panchayatname3.HasValue) yield return "Panchayatname3";
        if (dto.Panchayatname4.HasValue) yield return "Panchayatname4";
        if (dto.EmailCRM is not null) yield return "EmailCRM";
    }
}
