using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Data;

/// <summary>
/// EF Core DbContext that dynamically switches to the tenant database
/// based on the per-request ITenantContext connection string.
/// </summary>
public class SmreaderDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public SmreaderDbContext(DbContextOptions<SmreaderDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Dynamically switch to the tenant database if resolved
        if (_tenantContext.IsResolved && !string.IsNullOrEmpty(_tenantContext.ConnectionString))
        {
            optionsBuilder.UseMySql(
                _tenantContext.ConnectionString,
                new MySqlServerVersion(new Version(8, 0, 36)));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ca_users column mapping
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("ca_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerGuid).HasColumnName("owner_guid");
        });

        // ca_refresh_tokens column mapping
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("ca_refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
        });
    }
}
