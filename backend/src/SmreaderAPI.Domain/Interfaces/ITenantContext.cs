namespace SmreaderAPI.Domain.Interfaces;

/// <summary>
/// Per-request tenant context. Set by TenantResolutionMiddleware, consumed by data access layer.
/// </summary>
public interface ITenantContext
{
    int TenantId { get; }
    string ConnectionString { get; }
    bool IsResolved { get; }
    void Set(int tenantId, string connectionString);
}
