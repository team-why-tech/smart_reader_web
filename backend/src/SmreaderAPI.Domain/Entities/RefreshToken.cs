using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

/// <summary>
/// Maps to the ca_refresh_tokens table in tenant databases.
/// </summary>
[Table("ca_refresh_tokens")]
public class RefreshToken : BaseEntity
{
    [Column("user_id")]
    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }
}
