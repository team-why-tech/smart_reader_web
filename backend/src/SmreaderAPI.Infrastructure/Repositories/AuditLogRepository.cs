using System.Data;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.Repositories;

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(SmreaderDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
        : base(dbContext, connection, transaction) { }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, int entityId)
    {
        return await _dbSet.AsNoTracking()
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
    {
        return await _dbSet.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }
}
