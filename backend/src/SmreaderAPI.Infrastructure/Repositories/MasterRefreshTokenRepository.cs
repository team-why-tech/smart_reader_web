using Dapper;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Infrastructure.Repositories;

public class MasterRefreshTokenRepository : IMasterRefreshTokenRepository
{
    private readonly IMasterDbConnectionFactory _connectionFactory;

    public MasterRefreshTokenRepository(IMasterDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MasterRefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        const string sql = @"
SELECT id AS Id,
       user_id AS UserId,
       tenant_id AS TenantId,
       financial_year AS FinancialYear,
       token_hash AS TokenHash,
       expires_at AS ExpiresAt,
       created_at AS CreatedAt,
       revoked_at AS RevokedAt,
       replaced_by_token AS ReplacedByToken,
       ip_address AS IpAddress
FROM refresh_tokens
WHERE token_hash = @TokenHash
LIMIT 1;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MasterRefreshToken>(sql, new { TokenHash = tokenHash });
    }

    public async Task AddAsync(MasterRefreshToken token)
    {
        const string sql = @"
INSERT INTO refresh_tokens
(user_id, tenant_id, financial_year, token_hash, expires_at, created_at, revoked_at, replaced_by_token, ip_address)
VALUES
(@UserId, @TenantId, @FinancialYear, @TokenHash, @ExpiresAt, @CreatedAt, @RevokedAt, @ReplacedByToken, @IpAddress);
SELECT LAST_INSERT_ID();";

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.ExecuteScalarAsync<int>(sql, token);
        token.Id = id;
    }

    public async Task RevokeAsync(int id, string? replacedByTokenHash = null)
    {
        const string sql = @"
UPDATE refresh_tokens
SET revoked_at = @RevokedAt,
    replaced_by_token = @ReplacedByToken
WHERE id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            RevokedAt = DateTime.UtcNow,
            ReplacedByToken = replacedByTokenHash
        });
    }
}
