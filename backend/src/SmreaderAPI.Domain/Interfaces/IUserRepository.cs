using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
