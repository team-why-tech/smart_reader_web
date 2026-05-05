using SmreaderAPI.Domain.Interfaces.Master;

namespace SmreaderAPI.Domain.Interfaces;

/// <summary>
/// Unit of work for master database operations (tenants, tenant_databases, refresh_tokens)
/// </summary>
public interface IMasterUnitOfWork : IDisposable
{
    ITenantRepository Tenants { get; }
    ITenantDatabaseRepository TenantDatabases { get; }
    IMasterRefreshTokenRepository RefreshTokens { get; }

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
