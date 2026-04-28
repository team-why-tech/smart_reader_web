using SmreaderAPI.Application.DTOs;

namespace SmreaderAPI.Application.Interfaces;

public interface IAuditService
{
    Task LogActionAsync(int? userId, string action, string entityName, int? entityId, string? details = null);
    Task<ApiResponse<IEnumerable<AuditLogDto>>> GetByUserAsync(int userId);
    Task<ApiResponse<IEnumerable<AuditLogDto>>> GetAllAsync();
}
