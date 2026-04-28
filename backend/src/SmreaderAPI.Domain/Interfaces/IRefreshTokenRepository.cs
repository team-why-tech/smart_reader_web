using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeTokenAsync(int id);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId);
}
