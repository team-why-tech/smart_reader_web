namespace SmreaderAPI.Application.DTOs;

public record LoginDto(string Email, string Password, int TenantId, string? FinancialYear);
public record RegisterDto(string Name, string Email, string Password, int TenantId, string? FinancialYear);
public record TokenResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record RefreshTokenRequestDto(string RefreshToken);
public record SwitchFinancialYearDto(string FinancialYear);
