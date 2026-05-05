namespace SmreaderAPI.Application.DTOs;

public record LoginDto(string TenantCode, string Email, string Password);
public record RegisterDto(string Name, string Email, string Password);
public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequestDto(string RefreshToken);
public record SwitchFyDto(string FinancialYear);
