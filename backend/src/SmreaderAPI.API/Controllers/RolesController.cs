using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Mappings;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public RolesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var dtos = roles.Select(r => r.ToDto());
        return Ok(ApiResponse<IEnumerable<RoleDto>>.SuccessResponse(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);
        if (role is null)
            return NotFound(ApiResponse<RoleDto>.FailResponse("Role not found."));
        return Ok(ApiResponse<RoleDto>.SuccessResponse(role.ToDto()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
    {
        var existing = await _unitOfWork.Roles.GetByNameAsync(dto.Name);
        if (existing is not null)
            return BadRequest(ApiResponse<RoleDto>.FailResponse("Role already exists."));

        var role = new Role
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        role.Id = await _unitOfWork.Roles.AddAsync(role);
        return StatusCode(201, ApiResponse<RoleDto>.SuccessResponse(role.ToDto(), "Role created."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRoleDto dto)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);
        if (role is null)
            return NotFound(ApiResponse<RoleDto>.FailResponse("Role not found."));

        role.Name = dto.Name;
        role.Description = dto.Description;
        role.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Roles.UpdateAsync(role);
        return Ok(ApiResponse<RoleDto>.SuccessResponse(role.ToDto(), "Role updated."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);
        if (role is null)
            return NotFound(ApiResponse<bool>.FailResponse("Role not found."));

        await _unitOfWork.Roles.DeleteAsync(id);
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Role deleted."));
    }
}
