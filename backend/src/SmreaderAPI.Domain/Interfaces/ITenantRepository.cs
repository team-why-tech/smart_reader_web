using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Domain.Interfaces;

/// <summary>
/// Repository for querying the Master database (ca_management table).
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(int tenantId);
    Task<Tenant?> GetLatestByIdAsync(int tenantId);
    Task<IEnumerable<Tenant>> GetAllAsync();
}
