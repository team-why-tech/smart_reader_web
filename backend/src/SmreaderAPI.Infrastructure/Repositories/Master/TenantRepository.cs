using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities.Master;
using SmreaderAPI.Domain.Interfaces.Master;
using SmreaderAPI.Infrastructure.Data.Master;

namespace SmreaderAPI.Infrastructure.Repositories.Master;

public class TenantRepository : ITenantRepository
{
    private readonly MasterDbContext _dbContext;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public TenantRepository(MasterDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
    {
        _dbContext = dbContext;
        _connection = connection;
        _transaction = transaction;
    }

    public IQueryable<Tenant> AsQueryable() => _dbContext.Tenants.AsNoTracking();

    public async Task<Tenant?> GetByIdAsync(int id)
    {
        return await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tenant?> GetByCodeAsync(string code)
    {
        return await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Code == code);
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        return await _dbContext.Tenants.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<Tenant>> GetAllActiveAsync()
    {
        return await _dbContext.Tenants.AsNoTracking().Where(t => t.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Tenant>> FindAsync(System.Linq.Expressions.Expression<Func<Tenant, bool>> predicate)
    {
        return await _dbContext.Tenants.AsNoTracking().Where(predicate).ToListAsync();
    }

    public async Task<int> AddAsync(Tenant entity)
    {
        await _dbContext.Tenants.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<int> UpdateAsync(Tenant entity)
    {
        _dbContext.Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        _dbContext.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        var entity = await _dbContext.Tenants.FindAsync(id);
        if (entity is null) return 0;
        _dbContext.Tenants.Remove(entity);
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Tenant>> QueryAsync(string sql, object? param = null)
    {
        return await _connection.QueryAsync<Tenant>(sql, param, _transaction);
    }
}
