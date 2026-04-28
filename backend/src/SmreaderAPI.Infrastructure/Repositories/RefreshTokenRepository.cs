using System.Data;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(SmreaderDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
        : base(dbContext, connection, transaction) { }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task RevokeTokenAsync(int id)
    {
        var token = await _dbSet.FindAsync(id);
        if (token is not null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId)
    {
        return await _dbSet.AsNoTracking()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }
}
