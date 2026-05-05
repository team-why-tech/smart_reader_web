namespace SmreaderAPI.Domain.Interfaces;

/// <summary>
/// Unit of work for tenant database operations
/// Note: RefreshTokens are stored in Master DB via IMasterUnitOfWork
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IAuditLogRepository AuditLogs { get; }

    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
