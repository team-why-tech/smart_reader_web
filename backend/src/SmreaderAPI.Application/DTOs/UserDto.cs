namespace SmreaderAPI.Application.DTOs;

public record UserDto(int Id, string Name, string Email, string? Mobile, int Status);
public record CreateUserDto(string Name, string Email, string Password, string? Mobile);
public record UpdateUserDto(string? Name, string? Email, string? Mobile, int? Status);
