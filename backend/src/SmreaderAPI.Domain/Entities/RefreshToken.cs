using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

/// <summary>
/// Maps to the ca_refresh_tokens table in the Master database.
/// Stores tenant_id to associate the token with the correct tenant.
/// </summary>
[Table("ca_refresh_tokens")]
public class RefreshToken
{
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }
}
