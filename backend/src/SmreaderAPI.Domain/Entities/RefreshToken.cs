using System.ComponentModel.DataAnnotations.Schema;

namespace SmreaderAPI.Domain.Entities;

[Table("RefreshTokens")]
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked { get; set; }
}
