using Microsoft.Extensions.Logging;
using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Application.Mappings;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogActionAsync(int? userId, string action, string entityName, int? entityId, string? details = null)
    {
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

        await _unitOfWork.AuditLogs.AddAsync(auditLog);
        _logger.LogInformation("Audit: {Action} on {EntityName}:{EntityId} by User:{UserId}", action, entityName, entityId, userId);
    }

    public async Task<ApiResponse<IEnumerable<AuditLogDto>>> GetByUserAsync(int userId)
    {
        var logs = await _unitOfWork.AuditLogs.GetByUserIdAsync(userId);
        var dtos = logs.Select(l => l.ToDto());
        return ApiResponse<IEnumerable<AuditLogDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<IEnumerable<AuditLogDto>>> GetAllAsync()
    {
        var logs = await _unitOfWork.AuditLogs.GetAllAsync();
        var dtos = logs.Select(l => l.ToDto());
        return ApiResponse<IEnumerable<AuditLogDto>>.SuccessResponse(dtos);
    }
}
