using System.Data;
using MySqlConnector;
using SmreaderAPI.Application.Tenancy;

namespace SmreaderAPI.Infrastructure.Data;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(TenantContext tenantContext)
    {
        if (string.IsNullOrWhiteSpace(tenantContext.ConnectionString))
            throw new InvalidOperationException("Tenant connection string is not set.");

        _connectionString = tenantContext.ConnectionString;
    }

    public DapperContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
