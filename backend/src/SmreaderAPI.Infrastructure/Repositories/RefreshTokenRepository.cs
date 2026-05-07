using Dapper;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.Repositories;

/// <summary>
/// Queries the Master database (ca_refresh_tokens) using Dapper.
/// Uses CreateMasterConnection() — independent of tenant context.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DapperContext _dapperContext;

    public RefreshTokenRepository(DapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        using var connection = _dapperContext.CreateMasterConnection();
        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
            @"SELECT id AS Id, tenant_id AS TenantId, user_id AS UserId, token AS Token,
                     expires_at AS ExpiresAt, created_at AS CreatedAt,
                     revoked_at AS RevokedAt, is_revoked AS IsRevoked
              FROM ca_refresh_tokens WHERE token = @Token",
            new { Token = token });
    }

    public async Task<int> AddAsync(RefreshToken refreshToken)
    {
        using var connection = _dapperContext.CreateMasterConnection();
        return await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO ca_refresh_tokens (tenant_id, user_id, token, expires_at, created_at, is_revoked)
              VALUES (@TenantId, @UserId, @Token, @ExpiresAt, @CreatedAt, @IsRevoked);
              SELECT LAST_INSERT_ID();",
            new
            {
                refreshToken.TenantId,
                refreshToken.UserId,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                refreshToken.CreatedAt,
                refreshToken.IsRevoked
            });
    }

    public async Task RevokeTokenAsync(int id)
    {
        using var connection = _dapperContext.CreateMasterConnection();
        await connection.ExecuteAsync(
            "UPDATE ca_refresh_tokens SET is_revoked = 1, revoked_at = @RevokedAt WHERE id = @Id",
            new { Id = id, RevokedAt = DateTime.UtcNow });
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int tenantId, int userId)
    {
        using var connection = _dapperContext.CreateMasterConnection();
        return await connection.QueryAsync<RefreshToken>(
            @"SELECT id AS Id, tenant_id AS TenantId, user_id AS UserId, token AS Token,
                     expires_at AS ExpiresAt, created_at AS CreatedAt,
                     revoked_at AS RevokedAt, is_revoked AS IsRevoked
              FROM ca_refresh_tokens
              WHERE tenant_id = @TenantId AND user_id = @UserId
                AND is_revoked = 0 AND expires_at > @Now",
            new { TenantId = tenantId, UserId = userId, Now = DateTime.UtcNow });
    }
}
