using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities.Master;

[Table("tenant_databases")]
public class TenantDatabase : BaseEntity
{
    public int TenantId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
