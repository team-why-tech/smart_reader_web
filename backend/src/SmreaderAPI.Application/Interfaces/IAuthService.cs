using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Interfaces;

public interface IAuthService
{
    string GenerateJwtToken(User user, int tenantId);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string token);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Encrypts a string using AES-128-CTR to match the legacy PHP openssl_encrypt implementation.
    /// </summary>
    string LegacyEncrypt(string plainText);

    /// <summary>
    /// Verifies a plaintext password against a legacy PHP AES-128-CTR encrypted value.
    /// </summary>
    bool VerifyLegacyPassword(string password, string encryptedPassword);
}
