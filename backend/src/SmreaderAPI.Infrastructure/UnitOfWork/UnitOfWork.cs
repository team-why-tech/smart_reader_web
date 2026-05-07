using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;
using SmreaderAPI.Infrastructure.Repositories;

namespace SmreaderAPI.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly SmreaderDbContext _dbContext;
    private readonly DapperContext _dapperContext;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private IDbContextTransaction? _efTransaction;

    private IUserRepository? _users;
    private IAuditLogRepository? _auditLogs;

    public UnitOfWork(SmreaderDbContext dbContext, DapperContext dapperContext)
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

    public IUserRepository Users =>
        _users ??= new UserRepository(_dbContext, Connection, _transaction);

    public IAuditLogRepository AuditLogs =>
        _auditLogs ??= new AuditLogRepository(_dbContext, Connection, _transaction);

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
        _users = null;
        _auditLogs = null;
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
        GC.SuppressFinalize(this);
    }
}
