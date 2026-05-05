using System.Data;

namespace SmreaderAPI.Application.Interfaces;

public interface IMasterDbConnectionFactory
{
    IDbConnection CreateConnection();
}
