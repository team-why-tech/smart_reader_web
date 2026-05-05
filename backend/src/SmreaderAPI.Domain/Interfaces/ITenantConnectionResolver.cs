namespace SmreaderAPI.Domain.Interfaces;

/// <summary>
/// Resolves tenant database connection string from cache or master DB
/// </summary>
public interface ITenantConnectionResolver
{
    Task<string> ResolveConnectionStringAsync(int tenantId, string financialYear);
    Task InvalidateCacheAsync(int tenantId, string financialYear);
}
