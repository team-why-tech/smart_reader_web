namespace SmreaderAPI.Application.DTOs;

public record RoleDto(int Id, string Name, string? Description);
public record CreateRoleDto(string Name, string? Description);
