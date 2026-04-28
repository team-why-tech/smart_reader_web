using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

[Table("Roles")]
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
