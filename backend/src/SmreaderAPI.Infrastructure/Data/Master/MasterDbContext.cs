using Microsoft.EntityFrameworkCore;
using SmreaderAPI.Domain.Entities.Master;

namespace SmreaderAPI.Infrastructure.Data.Master;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantDatabase> TenantDatabases => Set<TenantDatabase>();
    public DbSet<MasterRefreshToken> RefreshTokens => Set<MasterRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table mappings come from [Table] attributes on entities
        // EF Core auto-detects Id as primary key by convention
        base.OnModelCreating(modelBuilder);
    }
}
