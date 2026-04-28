using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _auditService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _auditService.GetByUserAsync(userId);
        return Ok(result);
    }
}
