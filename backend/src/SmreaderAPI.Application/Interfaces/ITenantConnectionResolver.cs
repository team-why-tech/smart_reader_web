namespace SmreaderAPI.Application.Interfaces;

public interface ITenantConnectionResolver
{
    Task<string> ResolveConnectionStringAsync(int tenantId, string financialYear);
}
