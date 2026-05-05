using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Interfaces;

public interface IAuthService
{
    string GenerateJwtToken(User user, string roleName, int tenantId, string financialYear);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
