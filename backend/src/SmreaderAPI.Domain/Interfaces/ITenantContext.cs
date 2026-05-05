namespace SmreaderAPI.Domain.Interfaces;

/// <summary>
/// Provides tenant context for the current request (populated from JWT claims)
/// </summary>
public interface ITenantContext
{
    int TenantId { get; }
    string FinancialYear { get; }
    string ConnectionString { get; }
    bool IsResolved { get; }
    void SetContext(int tenantId, string financialYear, string connectionString);
}
