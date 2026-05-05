using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Domain.Interfaces.Master;
using SmreaderAPI.Infrastructure.Data.Master;
using SmreaderAPI.Infrastructure.Repositories.Master;

namespace SmreaderAPI.Infrastructure.UnitOfWork;

public class MasterUnitOfWork : IMasterUnitOfWork
{
    private readonly MasterDbContext _dbContext;
    private readonly MasterDapperContext _dapperContext;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private IDbContextTransaction? _efTransaction;

    private ITenantRepository? _tenants;
    private ITenantDatabaseRepository? _tenantDatabases;
    private IMasterRefreshTokenRepository? _refreshTokens;

    public MasterUnitOfWork(MasterDbContext dbContext, MasterDapperContext dapperContext)
    {
        _dbContext = dbContext;
        _dapperContext = dapperContext;
    }

    private IDbConnection Connection
    {
        get
        {
            if (_connection is null)
            {
                _connection = _dapperContext.CreateConnection();
                _connection.Open();
            }
            return _connection;
        }
    }

    public ITenantRepository Tenants =>
        _tenants ??= new TenantRepository(_dbContext, Connection, _transaction);

    public ITenantDatabaseRepository TenantDatabases =>
        _tenantDatabases ??= new TenantDatabaseRepository(_dbContext, Connection, _transaction);

    public IMasterRefreshTokenRepository RefreshTokens =>
        _refreshTokens ??= new MasterRefreshTokenRepository(_dbContext, Connection, _transaction);

    public async Task BeginTransactionAsync()
    {
        // Start EF Core transaction
        _efTransaction = await _dbContext.Database.BeginTransactionAsync();

        // Also prepare Dapper connection/transaction for raw SQL
        if (_connection is null)
        {
            _connection = _dapperContext.CreateConnection();
            _connection.Open();
        }
        _transaction = _connection.BeginTransaction();

        // Reset repositories so they pick up the new transaction
        _tenants = null;
        _tenantDatabases = null;
        _refreshTokens = null;
    }

    public async Task CommitAsync()
    {
        // Commit EF Core transaction
        if (_efTransaction is not null)
        {
            await _efTransaction.CommitAsync();
            await _efTransaction.DisposeAsync();
            _efTransaction = null;
        }

        // Commit Dapper transaction
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    public async Task RollbackAsync()
    {
        // Rollback EF Core transaction
        if (_efTransaction is not null)
        {
            await _efTransaction.RollbackAsync();
            await _efTransaction.DisposeAsync();
            _efTransaction = null;
        }

        // Rollback Dapper transaction
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _efTransaction?.Dispose();
        _transaction?.Dispose();
        _connection?.Dispose();
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
