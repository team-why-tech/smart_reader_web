namespace SmreaderAPI.Domain.Entities;

public class TenantDatabase
{
    public int TenantId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
