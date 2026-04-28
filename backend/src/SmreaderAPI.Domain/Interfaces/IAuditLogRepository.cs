using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, int entityId);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);
}
