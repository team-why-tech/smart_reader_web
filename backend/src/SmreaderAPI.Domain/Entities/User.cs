using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

/// <summary>
/// Maps to the ca_users table in tenant databases.
/// </summary>
[Table("ca_users")]
public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string Mobile { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Pwd { get; set; } = string.Empty;

    [Column("owner_guid")]
    public int OwnerGuid { get; set; }

    public int Status { get; set; }
}
