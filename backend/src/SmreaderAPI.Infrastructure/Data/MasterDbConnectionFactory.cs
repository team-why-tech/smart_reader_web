using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.Infrastructure.Data
{
    public class MasterDbConnectionFactory : IMasterDbConnectionFactory
    {
        private readonly string _connectionString;

        public MasterDbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MasterConnection") 
                ?? throw new InvalidOperationException("MasterConnection string is not configured.");
        }

        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }
    }
}
