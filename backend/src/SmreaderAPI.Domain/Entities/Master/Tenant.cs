using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities.Master;

[Table("tenants")]
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
