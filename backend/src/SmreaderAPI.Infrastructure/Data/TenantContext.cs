using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Data;

/// <summary>
/// Scoped service holding per-request tenant information.
/// Set by TenantResolutionMiddleware, consumed by DapperContext and SmreaderDbContext.
/// </summary>
public class TenantContext : ITenantContext
{
    public int TenantId { get; private set; }
    public string ConnectionString { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    public void Set(int tenantId, string connectionString)
    {
        TenantId = tenantId;
        ConnectionString = connectionString;
        IsResolved = true;
    }
}
