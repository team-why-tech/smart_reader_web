using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace SmreaderAPI.Infrastructure.Data.Master;

public class MasterDapperContext
{
    private readonly string _connectionString;

    public MasterDapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MasterConnection")
            ?? throw new InvalidOperationException("Connection string 'MasterConnection' not found.");
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
