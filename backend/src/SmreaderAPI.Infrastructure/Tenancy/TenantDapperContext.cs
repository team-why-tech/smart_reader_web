using System.Data;
using MySqlConnector;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Tenancy;

/// <summary>
/// Dapper connection factory for tenant databases (uses ITenantContext for dynamic connection)
/// </summary>
public class TenantDapperContext
{
    private readonly ITenantContext _tenantContext;

    public TenantDapperContext(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public IDbConnection CreateConnection()
    {
        if (!_tenantContext.IsResolved)
        {
            throw new InvalidOperationException("Tenant context not resolved. Ensure TenantResolutionMiddleware has run.");
        }

        return new MySqlConnection(_tenantContext.ConnectionString);
    }
}
