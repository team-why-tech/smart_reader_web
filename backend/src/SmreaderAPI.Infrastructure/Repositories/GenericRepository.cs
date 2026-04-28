using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.Repositories;

public class GenericRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly SmreaderDbContext _dbContext;
    protected readonly DbSet<T> _dbSet;
    protected readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;
    protected readonly string _tableName;

    public GenericRepository(SmreaderDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<T>();
        _connection = connection;
        _transaction = transaction;
        _tableName = GetTableName();
    }

    // EF Core — full LINQ support via IQueryable
    public IQueryable<T> AsQueryable() => _dbSet.AsNoTracking();

    // EF Core — native LINQ expression
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    // EF Core
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    // EF Core — native LINQ Where()
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    // EF Core
    public async Task<int> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    // EF Core
    public async Task<int> UpdateAsync(T entity)
    {
        _dbContext.Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        // Don't overwrite CreatedAt
        _dbContext.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        return await _dbContext.SaveChangesAsync();
    }

    // EF Core
    public async Task<int> DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is null) return 0;
        _dbSet.Remove(entity);
        return await _dbContext.SaveChangesAsync();
    }

    // Dapper — raw SQL pass-through for complex queries
    public async Task<IEnumerable<T>> QueryAsync(string sql, object? param = null)
    {
        return await _connection.QueryAsync<T>(sql, param, _transaction);
    }

    private static string GetTableName()
    {
        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
        return tableAttr?.Name ?? $"{typeof(T).Name}s";
    }
}
