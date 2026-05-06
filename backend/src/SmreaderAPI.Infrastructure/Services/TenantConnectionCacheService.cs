using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Infrastructure.Data;
using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.Infrastructure.Services;

public class TenantConnectionCacheService
{
    private readonly IMasterDbConnectionFactory _masterFactory;
    private readonly IMemoryCache _cache;

    public TenantConnectionCacheService(IMasterDbConnectionFactory masterFactory, IMemoryCache cache)
    {
        _masterFactory = masterFactory;
        _cache = cache;
    }

    public async Task<string?> GetConnectionStringAsync(int tenantId)
    {
        var cacheKey = $"TenantConn_{tenantId}";
        if (_cache.TryGetValue(cacheKey, out string? cachedConnectionString))
        {
            return cachedConnectionString;
        }

        using var connection = _masterFactory.CreateConnection();
        var tenant = await connection.QueryFirstOrDefaultAsync<TenantManagement>(
            "SELECT id as Id, name as Name, company_name as CompanyName, db_name as DbName, db_user as DbUser, db_pwd as DbPwd " +
            "FROM ca_management WHERE id = @Id", new { Id = tenantId });

        if (tenant == null || string.IsNullOrEmpty(tenant.DbName))
        {
            return null;
        }

        var masterConnStr = _masterFactory.GetConnectionString();
        var builder = new MySqlConnectionStringBuilder(masterConnStr)
        {
            Database = tenant.DbName,
            UserID = tenant.DbUser ?? "",
            Password = tenant.DbPwd ?? ""
        };

        var tenantConnStr = builder.ConnectionString;
        _cache.Set(cacheKey, tenantConnStr, TimeSpan.FromMinutes(30));

        return tenantConnStr;
    }
}
