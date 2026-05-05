using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Application.Interfaces;

public interface ITenantUnitOfWorkFactory
{
    IUnitOfWork Create(string connectionString);
}
