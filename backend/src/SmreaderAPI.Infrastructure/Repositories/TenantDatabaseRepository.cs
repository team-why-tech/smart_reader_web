using Dapper;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Infrastructure.Repositories;

public class TenantDatabaseRepository : ITenantDatabaseRepository
{
    private readonly IMasterDbConnectionFactory _connectionFactory;

    public TenantDatabaseRepository(IMasterDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TenantDatabase?> GetByTenantAndYearAsync(int tenantId, string financialYear)
    {
        const string sql = @"
SELECT tenant_id AS TenantId,
       financial_year AS FinancialYear,
       connection_string AS ConnectionString,
       is_default AS IsDefault
FROM tenant_databases
WHERE tenant_id = @TenantId AND financial_year = @FinancialYear
LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<TenantDatabase>(sql, new
        {
            TenantId = tenantId,
            FinancialYear = financialYear
        });
    }

    public async Task<TenantDatabase?> GetDefaultAsync(int tenantId)
    {
        const string sql = @"
SELECT tenant_id AS TenantId,
       financial_year AS FinancialYear,
       connection_string AS ConnectionString,
       is_default AS IsDefault
FROM tenant_databases
WHERE tenant_id = @TenantId AND is_default = 1
LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<TenantDatabase>(sql, new { TenantId = tenantId });
    }

    public async Task<bool> FinancialYearExistsAsync(int tenantId, string financialYear)
    {
        const string sql = @"
SELECT 1
FROM tenant_databases
WHERE tenant_id = @TenantId AND financial_year = @FinancialYear
LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QuerySingleOrDefaultAsync<int?>(sql, new
        {
            TenantId = tenantId,
            FinancialYear = financialYear
        });

        return result.HasValue;
    }
}
