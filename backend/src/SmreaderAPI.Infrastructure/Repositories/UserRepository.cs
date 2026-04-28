using System.Data;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(SmreaderDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
        : base(dbContext, connection, transaction) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(int roleId)
    {
        return await _dbSet.AsNoTracking().Where(u => u.RoleId == roleId).ToListAsync();
    }
}
