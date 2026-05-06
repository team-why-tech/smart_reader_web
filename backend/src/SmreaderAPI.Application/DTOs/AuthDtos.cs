namespace SmreaderAPI.Application.DTOs;

public record LoginDto(string Email, string Password);
public record TenantLoginDto(int TenantId, string Email, string Password);
public record RegisterDto(string Name, string Email, string Password);
public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequestDto(string RefreshToken);
