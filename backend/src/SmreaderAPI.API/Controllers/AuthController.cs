using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Tenancy;

namespace SmreaderAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _userService.RegisterAsync(dto);
        if (!result.Success)
            return BadRequest(result);
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _userService.LoginAsync(dto);
        if (!result.Success)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _userService.RefreshTokenAsync(dto);
        if (!result.Success)
            return Unauthorized(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("switch-fy")]
    public async Task<IActionResult> SwitchFinancialYear([FromBody] SwitchFinancialYearDto dto)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdValue = User.FindFirstValue(TenantClaimTypes.TenantId);
        if (!int.TryParse(userIdValue, out var userId) || !int.TryParse(tenantIdValue, out var tenantId))
            return Unauthorized(ApiResponse<TokenResponseDto>.FailResponse("Invalid token claims."));

        var result = await _userService.SwitchFinancialYearAsync(userId, tenantId, dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
