using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmreaderAPI.API.Controllers;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _sut = new UsersController(_userServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new(1, "Test", "test@test.com", "User", true, DateTime.UtcNow)
        };
        _userServiceMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(ApiResponse<IEnumerable<UserDto>>.SuccessResponse(users));

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsOk()
    {
        // Arrange
        var user = new UserDto(1, "Test", "test@test.com", "User", true, DateTime.UtcNow);
        _userServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ApiResponse<UserDto>.SuccessResponse(user));

        // Act
        var result = await _sut.GetById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync(ApiResponse<UserDto>.FailResponse("User not found."));

        // Act
        var result = await _sut.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingUser_ReturnsOk()
    {
        // Arrange
        _userServiceMock.Setup(x => x.DeleteAsync(1))
            .ReturnsAsync(ApiResponse<bool>.SuccessResponse(true, "User deleted."));

        // Act
        var result = await _sut.Delete(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
