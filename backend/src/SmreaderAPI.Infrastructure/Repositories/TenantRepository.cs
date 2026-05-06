using Dapper;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.Repositories;

/// <summary>
/// Queries the Master database (ca_management) using Dapper.
/// Does NOT go through EF Core — uses CreateMasterConnection() directly.
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly DapperContext _dapperContext;

    public TenantRepository(DapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task<Tenant?> GetByIdAsync(int tenantId)
    {
        using var connection = _dapperContext.CreateMasterConnection();
        return await connection.QueryFirstOrDefaultAsync<Tenant>(
            "SELECT id AS Id, name AS Name, company_name AS CompanyName, db_name AS DbName, " +
            "db_user AS DbUser, db_pwd AS DbPwd, date_from AS DateFrom, date_to AS DateTo " +
            "FROM ca_management WHERE id = @Id",
            new { Id = tenantId });
    }

    /// <summary>
    /// Gets the tenant with the latest financial year (highest date_to) for the given tenant ID.
    /// </summary>
    public async Task<Tenant?> GetLatestByIdAsync(int tenantId)
    {
        using var connection = _dapperContext.CreateMasterConnection();
        return await connection.QueryFirstOrDefaultAsync<Tenant>(
            "SELECT id AS Id, name AS Name, company_name AS CompanyName, db_name AS DbName, " +
            "db_user AS DbUser, db_pwd AS DbPwd, date_from AS DateFrom, date_to AS DateTo " +
            "FROM ca_management WHERE id = @Id ORDER BY date_to DESC LIMIT 1",
            new { Id = tenantId });
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        using var connection = _dapperContext.CreateMasterConnection();
        return await connection.QueryAsync<Tenant>(
            "SELECT id AS Id, name AS Name, company_name AS CompanyName, db_name AS DbName, " +
            "db_user AS DbUser, db_pwd AS DbPwd, date_from AS DateFrom, date_to AS DateTo " +
            "FROM ca_management");
    }
}
