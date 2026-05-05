using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Interfaces;

public interface ITenantDatabaseRepository
{
    Task<TenantDatabase?> GetByTenantAndYearAsync(int tenantId, string financialYear);
    Task<TenantDatabase?> GetDefaultAsync(int tenantId);
    Task<bool> FinancialYearExistsAsync(int tenantId, string financialYear);
}
