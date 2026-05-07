using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

/// <summary>
/// Maps to the ca_management table in the Master database.
/// Each row represents a tenant with its database credentials and financial year period.
/// </summary>
[Table("ca_management")]
public class Tenant
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [Column("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [Column("db_name")]
    public string? DbName { get; set; }

    [Column("db_user")]
    public string? DbUser { get; set; }

    [Column("db_pwd")]
    public string? DbPwd { get; set; }

    [Column("date_from")]
    public DateTime DateFrom { get; set; }

    [Column("date_to")]
    public DateTime DateTo { get; set; }
}
