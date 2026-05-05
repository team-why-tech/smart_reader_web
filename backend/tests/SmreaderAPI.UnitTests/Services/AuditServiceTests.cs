using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Services;
using SmreaderAPI.Application.Tenancy;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.UnitTests.Services;

public class AuditServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantUnitOfWorkFactory> _unitOfWorkFactoryMock;
    private readonly TenantContext _tenantContext;
    private readonly Mock<ILogger<AuditService>> _loggerMock;
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkFactoryMock = new Mock<ITenantUnitOfWorkFactory>();
        _tenantContext = new TenantContext();
        _loggerMock = new Mock<ILogger<AuditService>>();

        _tenantContext.Set(101, "2024-25", "Server=tenant-db;Database=TenantDb;");
        _unitOfWorkFactoryMock.Setup(x => x.Create(It.IsAny<string>())).Returns(_unitOfWorkMock.Object);

        _sut = new AuditService(_unitOfWorkFactoryMock.Object, _tenantContext, _loggerMock.Object);
    }

    [Fact]
    public async Task LogActionAsync_CreatesAuditLogEntry()
    {
        // Arrange
        _unitOfWorkMock.Setup(x => x.AuditLogs.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(1);

        // Act
        await _sut.LogActionAsync(1, "Create", "User", 1, "Test details");

        // Assert
        _unitOfWorkMock.Verify(x => x.AuditLogs.AddAsync(It.Is<AuditLog>(a =>
            a.UserId == 1 &&
            a.Action == "Create" &&
            a.EntityName == "User" &&
            a.EntityId == 1 &&
            a.Details == "Test details"
        )), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new() { Id = 1, UserId = 1, Action = "Create", EntityName = "User", EntityId = 1, Timestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 2, Action = "Update", EntityName = "User", EntityId = 2, Timestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        _unitOfWorkMock.Setup(x => x.AuditLogs.GetAllAsync()).ReturnsAsync(logs);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnUserLogs()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new() { Id = 1, UserId = 1, Action = "Login", EntityName = "User", EntityId = 1, Timestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        _unitOfWorkMock.Setup(x => x.AuditLogs.GetByUserIdAsync(1)).ReturnsAsync(logs);

        // Act
        var result = await _sut.GetByUserAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }
}
