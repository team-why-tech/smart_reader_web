using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.Infrastructure.Data;

public class MasterDbConnectionFactory : IMasterDbConnectionFactory
{
    private readonly string _connectionString;

    public MasterDbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MasterConnection")
            ?? throw new InvalidOperationException("Connection string 'MasterConnection' not found.");
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
