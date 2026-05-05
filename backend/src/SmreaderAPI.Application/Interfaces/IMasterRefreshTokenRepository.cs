using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Interfaces;

public interface IMasterRefreshTokenRepository
{
    Task<MasterRefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task AddAsync(MasterRefreshToken token);
    Task RevokeAsync(int id, string? replacedByTokenHash = null);
}
