using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Interfaces;

public interface IAuthService
{
    string GenerateJwtToken(User user, string roleName);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string token);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
