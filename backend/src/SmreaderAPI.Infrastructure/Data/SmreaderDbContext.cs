using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Services;

namespace SmreaderAPI.Infrastructure.Data;

public class SmreaderDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly TenantConnectionCacheService _tenantConnectionCache;

    public SmreaderDbContext(
        DbContextOptions<SmreaderDbContext> options,
        ITenantContext tenantContext,
        TenantConnectionCacheService tenantConnectionCache) : base(options)
    {
        _tenantContext = tenantContext;
        _tenantConnectionCache = tenantConnectionCache;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TenantManagement> Tenants => Set<TenantManagement>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_tenantContext.TenantId.HasValue)
        {
            var connStr = _tenantConnectionCache.GetConnectionStringAsync(_tenantContext.TenantId.Value).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(connStr))
            {
                optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));
            }
        }
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Database-first: schema managed by scripts/init.sql
        // Table mappings come from [Table] attributes on entities
        // EF Core auto-detects Id as primary key by convention
    }
}
