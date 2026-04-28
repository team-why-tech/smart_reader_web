using System.Data;
using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.Repositories;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(SmreaderDbContext dbContext, IDbConnection connection, IDbTransaction? transaction)
        : base(dbContext, connection, transaction) { }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);
    }
}
