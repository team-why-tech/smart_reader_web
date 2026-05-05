using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Tenancy;

/// <summary>
/// Request-scoped tenant context populated by TenantResolutionMiddleware
/// </summary>
public class TenantContext : ITenantContext
{
    public int TenantId { get; private set; }
    public string FinancialYear { get; private set; } = string.Empty;
    public string ConnectionString { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    public void SetContext(int tenantId, string financialYear, string connectionString)
    {
        TenantId = tenantId;
        FinancialYear = financialYear;
        ConnectionString = connectionString;
        IsResolved = true;
    }
}
