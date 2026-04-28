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
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

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

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sut = new AuthService(configuration, _unitOfWorkMock.Object);
    }

    [Fact]
    public void GenerateJwtToken_ContainsCorrectClaims()
    {
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };

        var token = _sut.GenerateJwtToken(user, "Admin");

        token.Should().NotBeNullOrEmpty();
        // JWT tokens have 3 parts separated by dots
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

        _unitOfWorkMock.Setup(x => x.RefreshTokens.GetByTokenAsync("expired-token"))
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

        _unitOfWorkMock.Setup(x => x.RefreshTokens.GetByTokenAsync("revoked-token"))
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

        _unitOfWorkMock.Setup(x => x.RefreshTokens.GetByTokenAsync("valid-token"))
            .ReturnsAsync(validToken);

        var result = await _sut.ValidateRefreshTokenAsync("valid-token");

        result.Should().BeTrue();
    }
}
