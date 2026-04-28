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
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _authServiceMock = new Mock<IAuthService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _sut = new UserService(
            _unitOfWorkMock.Object,
            _authServiceMock.Object,
            _cacheServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_Success_ReturnsToken()
    {
        // Arrange
        var dto = new RegisterDto("Test", "test@test.com", "Password123");
        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Roles.GetByNameAsync("User")).ReturnsAsync(new Role { Id = 2, Name = "User" });
        _unitOfWorkMock.Setup(x => x.Users.AddAsync(It.IsAny<User>())).ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.RefreshTokens.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync(1);
        _authServiceMock.Setup(x => x.HashPassword("Password123")).Returns("hashed");
        _authServiceMock.Setup(x => x.GenerateJwtToken(It.IsAny<User>(), "User")).Returns("jwt-token");
        _authServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        // Act
        var result = await _sut.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("jwt-token");
        result.Data.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFail()
    {
        // Arrange
        var dto = new RegisterDto("Test", "test@test.com", "Password123");
        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email))
            .ReturnsAsync(new User { Id = 1, Email = dto.Email });

        // Act
        var result = await _sut.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already registered");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var dto = new LoginDto("test@test.com", "Password123");
        var user = new User { Id = 1, Email = dto.Email, PasswordHash = "hashed", Name = "Test", RoleId = 2, IsActive = true };

        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.Roles.GetByIdAsync(2)).ReturnsAsync(new Role { Id = 2, Name = "User" });
        _unitOfWorkMock.Setup(x => x.RefreshTokens.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync(1);
        _authServiceMock.Setup(x => x.VerifyPassword("Password123", "hashed")).Returns(true);
        _authServiceMock.Setup(x => x.GenerateJwtToken(user, "User")).Returns("jwt-token");
        _authServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("jwt-token");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFail()
    {
        // Arrange
        var dto = new LoginDto("test@test.com", "WrongPassword");
        var user = new User { Id = 1, Email = dto.Email, PasswordHash = "hashed", IsActive = true };

        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _authServiceMock.Setup(x => x.VerifyPassword("WrongPassword", "hashed")).Returns(false);

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        _cacheServiceMock.Setup(x => x.GetAsync<UserDto>("user:1")).ReturnsAsync((UserDto?)null);
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "Test", Email = "test@test.com", RoleId = 2, IsActive = true, CreatedAt = DateTime.UtcNow });
        _unitOfWorkMock.Setup(x => x.Roles.GetByIdAsync(2))
            .ReturnsAsync(new Role { Id = 2, Name = "User" });

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Test");
        _cacheServiceMock.Verify(x => x.SetAsync("user:1", It.IsAny<UserDto>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCachedUser()
    {
        // Arrange
        var cachedUser = new UserDto(1, "Test", "test@test.com", "User", true, DateTime.UtcNow);
        _cacheServiceMock.Setup(x => x.GetAsync<UserDto>("user:1")).ReturnsAsync(cachedUser);

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
        _cacheServiceMock.Verify(x => x.RemoveAsync("user:1"), Times.Once);
    }
}
