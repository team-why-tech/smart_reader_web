using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Infrastructure.Data;

/// <summary>
/// Builds MySQL connection strings for tenant databases by combining
/// the host/port from the Master DB connection with tenant-specific credentials.
/// </summary>
public class TenantConnectionStringBuilder
{
    private readonly string _masterHost;
    private readonly uint _masterPort;

    public TenantConnectionStringBuilder(IConfiguration configuration)
    {
        var masterConnStr = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var csb = new MySqlConnectionStringBuilder(masterConnStr);
        _masterHost = csb.Server;
        _masterPort = csb.Port;
    }

    public string Build(Tenant tenant)
    {
        if (string.IsNullOrEmpty(tenant.DbName))
            throw new InvalidOperationException($"Tenant {tenant.Id} has no database name configured.");

        var csb = new MySqlConnectionStringBuilder
        {
            Server = _masterHost,
            Port = _masterPort,
            Database = tenant.DbName,
            UserID = tenant.DbUser ?? string.Empty,
            Password = tenant.DbPwd ?? string.Empty,
            SslMode = MySqlSslMode.Preferred
        };

        return csb.ConnectionString;
    }
}
