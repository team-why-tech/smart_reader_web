using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using SmreaderAPI.API.Controllers;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Tenancy;

namespace SmreaderAPI.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _sut = new AuthController(_userServiceMock.Object);
    }

    [Fact]
    public async Task Register_Success_Returns201()
    {
        // Arrange
        var dto = new RegisterDto("Test", "test@test.com", "Password123", 101, "2024-25");
        var tokenResponse = new TokenResponseDto("jwt", "refresh", DateTime.UtcNow.AddMinutes(30));
        _userServiceMock.Setup(x => x.RegisterAsync(dto))
            .ReturnsAsync(ApiResponse<TokenResponseDto>.SuccessResponse(tokenResponse, "Registration successful."));

        // Act
        var result = await _sut.Register(dto);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Login_Success_Returns200()
    {
        // Arrange
        var dto = new LoginDto("test@test.com", "Password123", 101, "2024-25");
        var tokenResponse = new TokenResponseDto("jwt", "refresh", DateTime.UtcNow.AddMinutes(30));
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
        var dto = new LoginDto("test@test.com", "WrongPassword", 101, "2024-25");
        _userServiceMock.Setup(x => x.LoginAsync(dto))
            .ReturnsAsync(ApiResponse<TokenResponseDto>.FailResponse("Invalid email or password."));

        // Act
        var result = await _sut.Login(dto);

        // Assert
        var objectResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task SwitchFinancialYear_Success_Returns200()
    {
        // Arrange
        var dto = new SwitchFinancialYearDto("2023-24");
        var tokenResponse = new TokenResponseDto("jwt", string.Empty, DateTime.UtcNow.AddMinutes(30));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(TenantClaimTypes.TenantId, "101")
        };

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };

        _userServiceMock.Setup(x => x.SwitchFinancialYearAsync(1, 101, dto))
            .ReturnsAsync(ApiResponse<TokenResponseDto>.SuccessResponse(tokenResponse));

        // Act
        var result = await _sut.SwitchFinancialYear(dto);

        // Assert
        var objectResult = result.Should().BeOfType<OkObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SwitchFinancialYear_MissingClaims_Returns401()
    {
        // Arrange
        var dto = new SwitchFinancialYearDto("2023-24");
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await _sut.SwitchFinancialYear(dto);

        // Assert
        var objectResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(401);
    }
}
