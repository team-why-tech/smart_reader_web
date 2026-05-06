using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

[Table("ca_users")]
public class User : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Column("mobile")]
    public string Mobile { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("pwd")]
    public string Pwd { get; set; } = string.Empty;

    [Column("owner_guid")]
    public int OwnerGuid { get; set; }

    [Column("status")]
    public int Status { get; set; }
}
