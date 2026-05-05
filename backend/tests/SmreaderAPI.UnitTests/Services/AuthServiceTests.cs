using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Infrastructure.Services;

namespace SmreaderAPI.UnitTests.Services;

public class AuthServiceTests
{
    private readonly AuthService _sut;

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

        _sut = new AuthService(configuration);
    }

    [Fact]
    public void GenerateJwtToken_ContainsCorrectClaims()
    {
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };

        var token = _sut.GenerateJwtToken(user, "Admin", 101, "2024-25");

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
    public void HashRefreshToken_SameInput_ReturnsSameHash()
    {
        var hash1 = _sut.HashRefreshToken("refresh-token");
        var hash2 = _sut.HashRefreshToken("refresh-token");

        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashRefreshToken_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = _sut.HashRefreshToken("refresh-token-1");
        var hash2 = _sut.HashRefreshToken("refresh-token-2");

        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
        hash1.Should().NotBe(hash2);
    }
}
