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

    public string? Privilages { get; set; }

    [Column("category_guid")]
    public int CategoryGuid { get; set; }

    [Column("last_sync_date")]
    public DateTime LastSyncDate { get; set; }

    [Column("van_sale")]
    public int VanSale { get; set; }

    public int Tech { get; set; }

    [Column("user_inactive")]
    public int UserInactive { get; set; }

    public int CollectionAgent { get; set; }

    public int SuperAdmin { get; set; }

    public int Printertype { get; set; }

    public int Moduletype { get; set; }

    public int Billnumber { get; set; }

    public int ReadBillnumber { get; set; }

    [Column("panchayatname")]
    public int Panchayatname { get; set; }

    [Column("panchayatname1")]
    public int Panchayatname1 { get; set; }

    [Column("panchayatname2")]
    public int Panchayatname2 { get; set; }

    [Column("panchayatname3")]
    public int Panchayatname3 { get; set; }

    [Column("panchayatname4")]
    public int Panchayatname4 { get; set; }

    public string EmailCRM { get; set; } = string.Empty;
}
