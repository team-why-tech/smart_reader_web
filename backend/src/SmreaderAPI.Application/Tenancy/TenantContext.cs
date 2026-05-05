namespace SmreaderAPI.Application.Tenancy;

public class TenantContext
{
    public int? TenantId { get; private set; }
    public string? FinancialYear { get; private set; }
    public string? ConnectionString { get; private set; }

    public void Set(int tenantId, string financialYear, string connectionString)
    {
        TenantId = tenantId;
        FinancialYear = financialYear;
        ConnectionString = connectionString;
    }

    public void Clear()
    {
        TenantId = null;
        FinancialYear = null;
        ConnectionString = null;
    }
}
