using SmreaderAPI.Domain.Entities.Master;

namespace SmreaderAPI.Domain.Interfaces.Master;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByCodeAsync(string code);
    Task<IEnumerable<Tenant>> GetAllActiveAsync();
}
