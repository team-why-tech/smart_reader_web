using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Services;

namespace SmreaderAPI.Infrastructure.Data;

public class DapperContext
{
    private readonly MasterDbConnectionFactory _masterFactory;
    private readonly ITenantContext _tenantContext;
    private readonly TenantConnectionCacheService _tenantConnectionCache;

    public DapperContext(
        MasterDbConnectionFactory masterFactory,
        ITenantContext tenantContext,
        TenantConnectionCacheService tenantConnectionCache)
    {
        _masterFactory = masterFactory;
        _tenantContext = tenantContext;
        _tenantConnectionCache = tenantConnectionCache;
    }

    public IDbConnection CreateConnection()
    {
        if (_tenantContext.TenantId.HasValue)
        {
            var connStr = _tenantConnectionCache.GetConnectionStringAsync(_tenantContext.TenantId.Value).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(connStr))
            {
                return new MySqlConnection(connStr);
            }
        }
        
        // Fallback or Master DB use-cases (if needed before tenant is resolved)
        return _masterFactory.CreateConnection();
    }
}
