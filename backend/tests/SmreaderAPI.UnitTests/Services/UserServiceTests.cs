using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Services;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _authServiceMock = new Mock<IAuthService>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _auditServiceMock = new Mock<IAuditService>();
        _tenantContextMock = new Mock<ITenantContext>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _tenantContextMock.Setup(x => x.TenantId).Returns(1);
        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);

        _sut = new UserService(
            _unitOfWorkMock.Object,
            _authServiceMock.Object,
            _refreshTokenRepoMock.Object,
            _cacheServiceMock.Object,
            _auditServiceMock.Object,
            _tenantContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var dto = new TenantLoginDto(1, "test@test.com", "Password123");
        var user = new User { Id = 1, Email = dto.Email, Pwd = "hashed", Name = "Test", Status = 1 };

        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _refreshTokenRepoMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync(1);
        _authServiceMock.Setup(x => x.VerifyPassword("Password123", "hashed")).Returns(true);
        _authServiceMock.Setup(x => x.GenerateJwtToken(user, 1)).Returns("jwt-token");
        _authServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("jwt-token");
        _refreshTokenRepoMock.Verify(x => x.AddAsync(It.Is<RefreshToken>(rt => rt.TenantId == 1)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFail()
    {
        // Arrange
        var dto = new TenantLoginDto(1, "test@test.com", "WrongPassword");
        var user = new User { Id = 1, Email = dto.Email, Pwd = "hashed", Status = 1 };

        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _authServiceMock.Setup(x => x.VerifyPassword("WrongPassword", "hashed")).Returns(false);

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_DeactivatedUser_ReturnsFail()
    {
        // Arrange
        var dto = new TenantLoginDto(1, "test@test.com", "Password123");
        var user = new User { Id = 1, Email = dto.Email, Pwd = "hashed", Status = 0 };

        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _authServiceMock.Setup(x => x.VerifyPassword("Password123", "hashed")).Returns(true);

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("deactivated");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        _cacheServiceMock.Setup(x => x.GetAsync<UserDto>("tenant:1:user:1")).ReturnsAsync((UserDto?)null);
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "Test", Email = "test@test.com", Status = 1 });

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Test");
        _cacheServiceMock.Verify(x => x.SetAsync("tenant:1:user:1", It.IsAny<UserDto>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCachedUser()
    {
        // Arrange
        var cachedUser = new UserDto(1, "Test", "test@test.com", "1234567890", null, 0, 1, null, 0, DateTime.UtcNow, DateTime.MinValue, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, "");
        _cacheServiceMock.Setup(x => x.GetAsync<UserDto>("tenant:1:user:1")).ReturnsAsync(cachedUser);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(cachedUser);
        _unitOfWorkMock.Verify(x => x.Users.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_ClearsCache()
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "Test" });
        _unitOfWorkMock.Setup(x => x.Users.DeleteAsync(1)).ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        _cacheServiceMock.Verify(x => x.RemoveAsync("tenant:1:user:1"), Times.Once);
    }
}
