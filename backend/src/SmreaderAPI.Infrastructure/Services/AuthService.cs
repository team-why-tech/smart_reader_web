using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public AuthService(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepo)
    {
        _configuration = configuration;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public string GenerateJwtToken(User user, int tenantId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "30");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("tenant_id", tenantId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return System.Convert.ToBase64String(randomBytes);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await _refreshTokenRepo.GetByTokenAsync(token);
        return refreshToken is not null && !refreshToken.IsRevoked && refreshToken.ExpiresAt > DateTime.UtcNow;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    /// <inheritdoc />
    public string LegacyEncrypt(string plainText)
    {
        // Matches PHP: openssl_encrypt($string, "AES-128-CTR", "GeeksforGeeks", 0, "1234567891011121")
        // Key: "GeeksforGeeks" padded/truncated to 16 bytes for AES-128
        // IV:  "1234567891011121" (exactly 16 bytes)
        // options=0 means base64 output (PHP default)

        var key = new byte[16];
        var keyBytes = Encoding.UTF8.GetBytes("GeeksforGeeks");
        Array.Copy(keyBytes, key, Math.Min(keyBytes.Length, 16));

        var iv = Encoding.UTF8.GetBytes("1234567891011121");
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var encrypted = AesCtrTransform(key, iv, plainBytes);
        return Convert.ToBase64String(encrypted);
    }

    /// <inheritdoc />
    public bool VerifyLegacyPassword(string password, string encryptedPassword)
    {
        // Encrypt the incoming password and compare with stored ciphertext
        var encrypted = LegacyEncrypt(password);
        return string.Equals(encrypted, encryptedPassword, StringComparison.Ordinal);
    }

    /// <summary>
    /// AES-CTR mode transform. .NET does not provide CTR mode natively,
    /// so we implement it using ECB mode on counter blocks + XOR.
    /// </summary>
    private static byte[] AesCtrTransform(byte[] key, byte[] iv, byte[] input)
    {
        var output = new byte[input.Length];
        var counter = (byte[])iv.Clone();

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Mode = System.Security.Cryptography.CipherMode.ECB;
        aes.Padding = System.Security.Cryptography.PaddingMode.None;
        aes.Key = key;

        using var encryptor = aes.CreateEncryptor();
        var counterBlock = new byte[16];
        var offset = 0;

        while (offset < input.Length)
        {
            // Encrypt the counter block
            encryptor.TransformBlock(counter, 0, 16, counterBlock, 0);

            // XOR plaintext with encrypted counter
            var blockSize = Math.Min(16, input.Length - offset);
            for (var i = 0; i < blockSize; i++)
            {
                output[offset + i] = (byte)(input[offset + i] ^ counterBlock[i]);
            }

            offset += blockSize;

            // Increment counter (big-endian)
            for (var i = 15; i >= 0; i--)
            {
                if (++counter[i] != 0) break;
            }
        }

        return output;
    }
}
