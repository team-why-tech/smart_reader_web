using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmreaderAPI.API.Controllers;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Caching;
using SmreaderAPI.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmreaderAPI.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ITenantRepository> _tenantRepoMock;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _tenantContextMock = new Mock<ITenantContext>();
        _tenantRepoMock = new Mock<ITenantRepository>();

        var configData = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;Port=3306;Database=master;Uid=root;Pwd=pass;" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var connBuilder = new TenantConnectionStringBuilder(configuration);
        var cache = new TenantConnectionStringCache(
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<TenantConnectionStringCache>>().Object);

        _sut = new AuthController(
            _userServiceMock.Object,
            _tenantContextMock.Object,
            _tenantRepoMock.Object,
            connBuilder,
            cache);
    }

    [Fact]
    public async Task Login_Success_Returns200()
    {
        // Arrange
        var dto = new TenantLoginDto(1, "test@test.com", "Password123");
        var tenant = new Tenant { Id = 1, DbName = "testdb", DbUser = "user", DbPwd = "pwd" };
        var tokenResponse = new TokenResponseDto("jwt", "refresh", DateTime.UtcNow.AddMinutes(30));

        _tenantRepoMock.Setup(x => x.GetLatestByIdAsync(1)).ReturnsAsync(tenant);
        _userServiceMock.Setup(x => x.LoginAsync(dto))
            .ReturnsAsync(ApiResponse<TokenResponseDto>.SuccessResponse(tokenResponse));

        // Act
        var result = await _sut.Login(dto);

        // Assert
        var objectResult = result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        // Arrange
        var dto = new TenantLoginDto(1, "test@test.com", "WrongPassword");
        var tenant = new Tenant { Id = 1, DbName = "testdb", DbUser = "user", DbPwd = "pwd" };

        _tenantRepoMock.Setup(x => x.GetLatestByIdAsync(1)).ReturnsAsync(tenant);
        _userServiceMock.Setup(x => x.LoginAsync(dto))
            .ReturnsAsync(ApiResponse<TokenResponseDto>.FailResponse("Invalid email or password."));

        // Act
        var result = await _sut.Login(dto);

        // Assert
        var objectResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_TenantNotFound_Returns404()
    {
        // Arrange
        var dto = new TenantLoginDto(999, "test@test.com", "Password123");
        _tenantRepoMock.Setup(x => x.GetLatestByIdAsync(999)).ReturnsAsync((Tenant?)null);

        // Act
        var result = await _sut.Login(dto);

        // Assert
        var objectResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }
}
