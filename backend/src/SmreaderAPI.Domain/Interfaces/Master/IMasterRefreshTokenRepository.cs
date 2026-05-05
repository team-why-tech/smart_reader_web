using SmreaderAPI.Domain.Entities.Master;

namespace SmreaderAPI.Domain.Interfaces.Master;

public interface IMasterRefreshTokenRepository : IRepository<MasterRefreshToken>
{
    Task<MasterRefreshToken?> GetByTokenAsync(string token);
    Task RevokeTokenAsync(int id, string? replacedByToken = null);
    Task<IEnumerable<MasterRefreshToken>> GetActiveTokensByUserAndTenantAsync(int userId, int tenantId);
}
