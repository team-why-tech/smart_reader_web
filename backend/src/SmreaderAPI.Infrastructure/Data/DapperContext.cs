using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Data;

/// <summary>
/// Creates MySQL connections for both Master and Tenant databases.
/// Tenant connection uses the per-request ITenantContext.
/// Master connection uses the fixed DefaultConnection from appsettings.
/// </summary>
public class DapperContext
{
    private readonly string _masterConnectionString;
    private readonly ITenantContext _tenantContext;

    public DapperContext(IConfiguration configuration, ITenantContext tenantContext)
    {
        _masterConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Creates a connection to the current tenant's database.
    /// Requires ITenantContext to be resolved (set by middleware).
    /// </summary>
    public IDbConnection CreateConnection()
    {
        if (!_tenantContext.IsResolved)
            throw new InvalidOperationException("Tenant context not resolved. Ensure TenantResolutionMiddleware has executed.");

        return new MySqlConnection(_tenantContext.ConnectionString);
    }

    /// <summary>
    /// Creates a connection to the Master database (ca_management).
    /// Used for tenant resolution — does not require ITenantContext.
    /// </summary>
    public IDbConnection CreateMasterConnection() => new MySqlConnection(_masterConnectionString);
}
