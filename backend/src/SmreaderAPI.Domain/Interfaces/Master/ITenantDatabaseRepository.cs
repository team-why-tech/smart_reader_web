using SmreaderAPI.Domain.Entities.Master;

namespace SmreaderAPI.Domain.Interfaces.Master;

public interface ITenantDatabaseRepository : IRepository<TenantDatabase>
{
    Task<TenantDatabase?> GetByTenantAndFyAsync(int tenantId, string financialYear);
    Task<TenantDatabase?> GetDefaultForTenantAsync(int tenantId);
    Task<IEnumerable<TenantDatabase>> GetAllByTenantAsync(int tenantId);
}
