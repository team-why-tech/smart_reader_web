using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Domain.Interfaces;

/// <summary>
/// Standalone repository for refresh tokens in the Master database.
/// Not part of IUnitOfWork since it operates on a different database.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<int> AddAsync(RefreshToken refreshToken);
    Task RevokeTokenAsync(int id);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int tenantId, int userId);
}
