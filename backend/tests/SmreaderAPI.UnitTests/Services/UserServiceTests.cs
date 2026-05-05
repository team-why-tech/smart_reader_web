using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Services;
using SmreaderAPI.Application.Tenancy;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantUnitOfWorkFactory> _unitOfWorkFactoryMock;
    private readonly Mock<ITenantConnectionResolver> _tenantConnectionResolverMock;
    private readonly Mock<ITenantDatabaseRepository> _tenantDatabaseRepositoryMock;
    private readonly Mock<IMasterRefreshTokenRepository> _masterRefreshTokenRepositoryMock;
    private readonly TenantContext _tenantContext;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkFactoryMock = new Mock<ITenantUnitOfWorkFactory>();
        _tenantConnectionResolverMock = new Mock<ITenantConnectionResolver>();
        _tenantDatabaseRepositoryMock = new Mock<ITenantDatabaseRepository>();
        _masterRefreshTokenRepositoryMock = new Mock<IMasterRefreshTokenRepository>();
        _tenantContext = new TenantContext();
        _authServiceMock = new Mock<IAuthService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<UserService>>();

        var configData = new Dictionary<string, string?>
        {
            { "Jwt:AccessTokenExpiryMinutes", "30" },
            { "Jwt:RefreshTokenExpiryDays", "7" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);

        _unitOfWorkFactoryMock.Setup(x => x.Create(It.IsAny<string>())).Returns(_unitOfWorkMock.Object);
        _tenantConnectionResolverMock.Setup(x => x.ResolveConnectionStringAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("Server=tenant-db;Database=TenantDb;");
        _tenantDatabaseRepositoryMock.Setup(x => x.GetDefaultAsync(It.IsAny<int>()))
            .ReturnsAsync(new TenantDatabase
            {
                TenantId = 101,
                FinancialYear = "2024-25",
                ConnectionString = "Server=tenant-db;Database=TenantDb;",
                IsDefault = true
            });
        _masterRefreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<MasterRefreshToken>()))
            .Returns(Task.CompletedTask);

        _tenantContext.Set(101, "2024-25", "Server=tenant-db;Database=TenantDb;");

        _sut = new UserService(
            _unitOfWorkFactoryMock.Object,
            _tenantConnectionResolverMock.Object,
            _tenantDatabaseRepositoryMock.Object,
            _masterRefreshTokenRepositoryMock.Object,
            _tenantContext,
            _authServiceMock.Object,
            _cacheServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object,
            configuration);
    }

    [Fact]
    public async Task RegisterAsync_Success_ReturnsToken()
    {
        // Arrange
        var dto = new RegisterDto("Test", "test@test.com", "Password123", 101, "2024-25");
        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Roles.GetByNameAsync("User")).ReturnsAsync(new Role { Id = 2, Name = "User" });
        _unitOfWorkMock.Setup(x => x.Users.AddAsync(It.IsAny<User>())).ReturnsAsync(1);
        _authServiceMock.Setup(x => x.HashPassword("Password123")).Returns("hashed");
        _authServiceMock.Setup(x => x.GenerateJwtToken(It.IsAny<User>(), "User", 101, "2024-25")).Returns("jwt-token");
        _authServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _authServiceMock.Setup(x => x.HashRefreshToken("refresh-token")).Returns("refresh-hash");

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
        var dto = new RegisterDto("Test", "test@test.com", "Password123", 101, "2024-25");
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
        var dto = new LoginDto("test@test.com", "Password123", 101, "2024-25");
        var user = new User { Id = 1, Email = dto.Email, PasswordHash = "hashed", Name = "Test", RoleId = 2, IsActive = true };

        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.Roles.GetByIdAsync(2)).ReturnsAsync(new Role { Id = 2, Name = "User" });
        _authServiceMock.Setup(x => x.VerifyPassword("Password123", "hashed")).Returns(true);
        _authServiceMock.Setup(x => x.GenerateJwtToken(user, "User", 101, "2024-25")).Returns("jwt-token");
        _authServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _authServiceMock.Setup(x => x.HashRefreshToken("refresh-token")).Returns("refresh-hash");

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
        var dto = new LoginDto("test@test.com", "WrongPassword", 101, "2024-25");
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
        _cacheServiceMock.Setup(x => x.GetAsync<UserDto>("tenant:101:user:1")).ReturnsAsync((UserDto?)null);
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "Test", Email = "test@test.com", RoleId = 2, IsActive = true, CreatedAt = DateTime.UtcNow });
        _unitOfWorkMock.Setup(x => x.Roles.GetByIdAsync(2))
            .ReturnsAsync(new Role { Id = 2, Name = "User" });

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Test");
        _cacheServiceMock.Verify(x => x.SetAsync("tenant:101:user:1", It.IsAny<UserDto>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCachedUser()
    {
        // Arrange
        var cachedUser = new UserDto(1, "Test", "test@test.com", "User", true, DateTime.UtcNow);
        _cacheServiceMock.Setup(x => x.GetAsync<UserDto>("tenant:101:user:1")).ReturnsAsync(cachedUser);

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
        _cacheServiceMock.Verify(x => x.RemoveAsync("tenant:101:user:1"), Times.Once);
    }
}
