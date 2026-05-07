using Microsoft.AspNetCore.Mvc;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Caching;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly TenantConnectionStringBuilder _connBuilder;
    private readonly TenantConnectionStringCache _connCache;

    public AuthController(
        IUserService userService,
        ITenantContext tenantContext,
        ITenantRepository tenantRepository,
        TenantConnectionStringBuilder connBuilder,
        TenantConnectionStringCache connCache)
    {
        _userService = userService;
        _tenantContext = tenantContext;
        _tenantRepository = tenantRepository;
        _connBuilder = connBuilder;
        _connCache = connCache;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] TenantLoginDto dto)
    {
        // Manually resolve tenant for login (middleware skips /auth/login)
        var connStr = _connCache.Get(dto.TenantId);
        if (connStr is null)
        {
            var tenant = await _tenantRepository.GetLatestByIdAsync(dto.TenantId);
            if (tenant is null)
                return NotFound(ApiResponse<TokenResponseDto>.FailResponse("Tenant not found."));

            connStr = _connBuilder.Build(tenant);
            _connCache.Set(dto.TenantId, connStr);
        }

        // Set tenant context so UnitOfWork/Repositories connect to the right DB
        _tenantContext.Set(dto.TenantId, connStr);

        var result = await _userService.LoginAsync(dto);
        if (!result.Success)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        // Tenant context is already set by TenantResolutionMiddleware (JWT has tenant_id)
        var result = await _userService.RefreshTokenAsync(dto);
        if (!result.Success)
            return Unauthorized(result);
        return Ok(result);
    }
}
