using Microsoft.Extensions.Logging;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Mappings;
using SmreaderAPI.Application.Tenancy;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Services;

public class AuditService : IAuditService
{
    private readonly ITenantUnitOfWorkFactory _unitOfWorkFactory;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ITenantUnitOfWorkFactory unitOfWorkFactory,
        TenantContext tenantContext,
        ILogger<AuditService> logger)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task LogActionAsync(int? userId, string action, string entityName, int? entityId, string? details = null)
    {
        using var unitOfWork = _unitOfWorkFactory.Create(GetTenantConnectionStringOrThrow());
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.AuditLogs.AddAsync(auditLog);
        _logger.LogInformation("Audit: {Action} on {EntityName}:{EntityId} by User:{UserId}", action, entityName, entityId, userId);
    }

    public async Task<ApiResponse<IEnumerable<AuditLogDto>>> GetByUserAsync(int userId)
    {
        using var unitOfWork = _unitOfWorkFactory.Create(GetTenantConnectionStringOrThrow());
        var logs = await unitOfWork.AuditLogs.GetByUserIdAsync(userId);
        var dtos = logs.Select(l => l.ToDto());
        return ApiResponse<IEnumerable<AuditLogDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<IEnumerable<AuditLogDto>>> GetAllAsync()
    {
        using var unitOfWork = _unitOfWorkFactory.Create(GetTenantConnectionStringOrThrow());
        var logs = await unitOfWork.AuditLogs.GetAllAsync();
        var dtos = logs.Select(l => l.ToDto());
        return ApiResponse<IEnumerable<AuditLogDto>>.SuccessResponse(dtos);
    }

    private string GetTenantConnectionStringOrThrow()
    {
        if (string.IsNullOrWhiteSpace(_tenantContext.ConnectionString))
            throw new InvalidOperationException("Tenant connection string is not set for this request.");

        return _tenantContext.ConnectionString;
    }
}
