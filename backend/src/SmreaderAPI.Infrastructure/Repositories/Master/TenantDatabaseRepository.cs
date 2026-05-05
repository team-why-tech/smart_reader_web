using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities.Master;
using SmreaderAPI.Domain.Interfaces.Master;
using SmreaderAPI.Infrastructure.Data.Master;

namespace SmreaderAPI.Infrastructure.Repositories.Master;

public class TenantDatabaseRepository : ITenantDatabaseRepository
{
    private readonly MasterDbContext _dbContext;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public TenantDatabaseRepository(MasterDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
    {
        _dbContext = dbContext;
        _connection = connection;
        _transaction = transaction;
    }

    public IQueryable<TenantDatabase> AsQueryable() => _dbContext.TenantDatabases.AsNoTracking();

    public async Task<TenantDatabase?> GetByIdAsync(int id)
    {
        return await _dbContext.TenantDatabases.AsNoTracking().FirstOrDefaultAsync(td => td.Id == id);
    }

    public async Task<TenantDatabase?> GetByTenantAndFyAsync(int tenantId, string financialYear)
    {
        return await _dbContext.TenantDatabases.AsNoTracking()
            .FirstOrDefaultAsync(td => td.TenantId == tenantId && td.FinancialYear == financialYear);
    }

    public async Task<TenantDatabase?> GetDefaultForTenantAsync(int tenantId)
    {
        return await _dbContext.TenantDatabases.AsNoTracking()
            .FirstOrDefaultAsync(td => td.TenantId == tenantId && td.IsDefault);
    }

    public async Task<IEnumerable<TenantDatabase>> GetAllByTenantAsync(int tenantId)
    {
        return await _dbContext.TenantDatabases.AsNoTracking()
            .Where(td => td.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<IEnumerable<TenantDatabase>> GetAllAsync()
    {
        return await _dbContext.TenantDatabases.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<TenantDatabase>> FindAsync(System.Linq.Expressions.Expression<Func<TenantDatabase, bool>> predicate)
    {
        return await _dbContext.TenantDatabases.AsNoTracking().Where(predicate).ToListAsync();
    }

    public async Task<int> AddAsync(TenantDatabase entity)
    {
        await _dbContext.TenantDatabases.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<int> UpdateAsync(TenantDatabase entity)
    {
        _dbContext.Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        _dbContext.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        var entity = await _dbContext.TenantDatabases.FindAsync(id);
        if (entity is null) return 0;
        _dbContext.TenantDatabases.Remove(entity);
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<TenantDatabase>> QueryAsync(string sql, object? param = null)
    {
        return await _connection.QueryAsync<TenantDatabase>(sql, param, _transaction);
    }
}
