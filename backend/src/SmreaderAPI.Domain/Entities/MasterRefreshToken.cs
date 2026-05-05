namespace SmreaderAPI.Domain.Entities;

public class MasterRefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? IpAddress { get; set; }
}
