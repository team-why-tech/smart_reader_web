using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Infrastructure.Data;

/// <summary>
/// DbContext for tenant databases (uses dynamic connection string via ITenantContext)
/// Note: RefreshTokens are stored in Master DB, not tenant DBs
/// </summary>
public class SmreaderDbContext : DbContext
{
    public SmreaderDbContext(DbContextOptions<SmreaderDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Database-first: schema managed by scripts/tenant_init.sql
        // Table mappings come from [Table] attributes on entities
        // EF Core auto-detects Id as primary key by convention
    }
}
