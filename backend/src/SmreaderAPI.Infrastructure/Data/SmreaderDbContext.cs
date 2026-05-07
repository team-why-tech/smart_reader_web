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
        if (_tenantContext.IsResolved && !string.IsNullOrEmpty(_tenantContext.ConnectionString))
        {
            Database.SetConnectionString(_tenantContext.ConnectionString);
        }
        else
        {
            // For endpoints like /auth/login where DbContext is injected before the tenant is resolved manually
            _tenantContext.OnTenantResolved += (connStr) => 
            {
                Database.SetConnectionString(connStr);
            };
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ca_users column mapping
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("ca_users");
            entity.HasKey(e => e.Id);

            // BaseEntity mappings
            entity.Property(e => e.CreatedAt).HasColumnName("post_date");
            entity.Ignore(e => e.UpdatedAt); // ca_users has no UpdatedAt column

            // Explicit column mappings
            entity.Property(e => e.OwnerGuid).HasColumnName("owner_guid");
            entity.Property(e => e.CategoryGuid).HasColumnName("category_guid");
            entity.Property(e => e.LastSyncDate).HasColumnName("last_sync_date");
            entity.Property(e => e.VanSale).HasColumnName("van_sale");
            entity.Property(e => e.UserInactive).HasColumnName("user_inactive");
            entity.Property(e => e.Panchayatname).HasColumnName("panchayatname");
            entity.Property(e => e.Panchayatname1).HasColumnName("panchayatname1");
            entity.Property(e => e.Panchayatname2).HasColumnName("panchayatname2");
            entity.Property(e => e.Panchayatname3).HasColumnName("panchayatname3");
            entity.Property(e => e.Panchayatname4).HasColumnName("panchayatname4");
        });

    }
}
