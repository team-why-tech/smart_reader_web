using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Services;

namespace SmreaderAPI.UnitTests.Services;

public class AuthServiceTests
{
    private readonly AuthService _sut;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;

    public AuthServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Key", "ThisIsASecretKeyThatIsAtLeast32CharactersLong!!" },
            { "Jwt:Issuer", "SmreaderAPI" },
            { "Jwt:Audience", "SmreaderAPI" },
            { "Jwt:AccessTokenExpiryMinutes", "30" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _sut = new AuthService(configuration, _refreshTokenRepoMock.Object);
    }

    [Fact]
    public void GenerateJwtToken_ContainsCorrectClaims()
    {
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };

        var token = _sut.GenerateJwtToken(user, 1);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ExpiredToken_ReturnsFalse()
    {
        var expiredToken = new RefreshToken
        {
            Id = 1,
            Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        _refreshTokenRepoMock.Setup(x => x.GetByTokenAsync("expired-token"))
            .ReturnsAsync(expiredToken);

        var result = await _sut.ValidateRefreshTokenAsync("expired-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_RevokedToken_ReturnsFalse()
    {
        var revokedToken = new RefreshToken
        {
            Id = 1,
            Token = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true
        };

        _refreshTokenRepoMock.Setup(x => x.GetByTokenAsync("revoked-token"))
            .ReturnsAsync(revokedToken);

        var result = await _sut.ValidateRefreshTokenAsync("revoked-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ReturnsTrue()
    {
        var validToken = new RefreshToken
        {
            Id = 1,
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _refreshTokenRepoMock.Setup(x => x.GetByTokenAsync("valid-token"))
            .ReturnsAsync(validToken);

        var result = await _sut.ValidateRefreshTokenAsync("valid-token");

        result.Should().BeTrue();
    }
}
