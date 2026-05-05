using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities.Master;

[Table("refresh_tokens")]
public class MasterRefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? IpAddress { get; set; }
}
