namespace SmreaderAPI.Application.DTOs;

public record LoginDto(string Email, string Password, int TenantId);
public record RegisterDto(string Name, string Email, string Password, string Mobile, string? Address, int TenantId);
public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequestDto(string RefreshToken, int TenantId);
