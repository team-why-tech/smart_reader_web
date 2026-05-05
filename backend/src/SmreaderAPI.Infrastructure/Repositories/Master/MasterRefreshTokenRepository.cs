using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities.Master;
using SmreaderAPI.Domain.Interfaces.Master;
using SmreaderAPI.Infrastructure.Data.Master;

namespace SmreaderAPI.Infrastructure.Repositories.Master;

public class MasterRefreshTokenRepository : IMasterRefreshTokenRepository
{
    private readonly MasterDbContext _dbContext;
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public MasterRefreshTokenRepository(MasterDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
    {
        _dbContext = dbContext;
        _connection = connection;
        _transaction = transaction;
    }

    public IQueryable<MasterRefreshToken> AsQueryable() => _dbContext.RefreshTokens.AsNoTracking();

    public async Task<MasterRefreshToken?> GetByIdAsync(int id)
    {
        return await _dbContext.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(rt => rt.Id == id);
    }

    public async Task<MasterRefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbContext.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<IEnumerable<MasterRefreshToken>> GetActiveTokensByUserAndTenantAsync(int userId, int tenantId)
    {
        return await _dbContext.RefreshTokens.AsNoTracking()
            .Where(rt => rt.UserId == userId && rt.TenantId == tenantId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task RevokeTokenAsync(int id, string? replacedByToken = null)
    {
        var token = await _dbContext.RefreshTokens.FindAsync(id);
        if (token is not null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.ReplacedByToken = replacedByToken;
            token.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<MasterRefreshToken>> GetAllAsync()
    {
        return await _dbContext.RefreshTokens.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<MasterRefreshToken>> FindAsync(System.Linq.Expressions.Expression<Func<MasterRefreshToken, bool>> predicate)
    {
        return await _dbContext.RefreshTokens.AsNoTracking().Where(predicate).ToListAsync();
    }

    public async Task<int> AddAsync(MasterRefreshToken entity)
    {
        await _dbContext.RefreshTokens.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<int> UpdateAsync(MasterRefreshToken entity)
    {
        _dbContext.Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        _dbContext.Entry(entity).Property(e => e.CreatedAt).IsModified = false;
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        var entity = await _dbContext.RefreshTokens.FindAsync(id);
        if (entity is null) return 0;
        _dbContext.RefreshTokens.Remove(entity);
        return await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<MasterRefreshToken>> QueryAsync(string sql, object? param = null)
    {
        return await _connection.QueryAsync<MasterRefreshToken>(sql, param, _transaction);
    }
}
