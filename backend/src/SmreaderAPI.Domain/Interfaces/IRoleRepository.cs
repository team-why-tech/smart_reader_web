using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Domain.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
}
