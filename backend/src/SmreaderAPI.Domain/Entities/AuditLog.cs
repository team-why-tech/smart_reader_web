using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

[Table("AuditLogs")]
public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}
