namespace SmreaderAPI.Application.DTOs;

public record UserDto(int Id, string Name, string Email, string RoleName, bool IsActive, DateTime CreatedAt);
public record CreateUserDto(string Name, string Email, string Password, int RoleId);
public record UpdateUserDto(string? Name, string? Email, int? RoleId, bool? IsActive);
