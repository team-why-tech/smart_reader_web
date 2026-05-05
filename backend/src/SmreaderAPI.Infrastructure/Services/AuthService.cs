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
    private readonly IMasterUnitOfWork _masterUnitOfWork;

    public AuthService(IConfiguration configuration, IMasterUnitOfWork masterUnitOfWork)
    {
        _configuration = configuration;
        _masterUnitOfWork = masterUnitOfWork;
    }

    public string GenerateJwtToken(User user, string roleName, int tenantId, string financialYear)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "30");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("fy", financialYear)
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
        var refreshToken = await _masterUnitOfWork.RefreshTokens.GetByTokenAsync(token);
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
}
