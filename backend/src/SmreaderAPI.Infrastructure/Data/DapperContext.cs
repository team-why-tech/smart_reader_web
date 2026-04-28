using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SmreaderAPI.Infrastructure.Data;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
