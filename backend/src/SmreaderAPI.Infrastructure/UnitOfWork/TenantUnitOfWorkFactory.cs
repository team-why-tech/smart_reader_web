using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Domain.Interfaces;
using SmreaderAPI.Infrastructure.Data;

namespace SmreaderAPI.Infrastructure.UnitOfWork;

public class TenantUnitOfWorkFactory : ITenantUnitOfWorkFactory
{
    private static readonly MySqlServerVersion MySqlVersion = new(new Version(8, 0, 36));
    private readonly IHostEnvironment _environment;

    public TenantUnitOfWorkFactory(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public IUnitOfWork Create(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SmreaderDbContext>();
        optionsBuilder.UseMySql(connectionString, MySqlVersion)
            .EnableSensitiveDataLogging(_environment.IsDevelopment())
            .EnableDetailedErrors(_environment.IsDevelopment());

        var dbContext = new SmreaderDbContext(optionsBuilder.Options);
        var dapperContext = new DapperContext(connectionString);

        return new UnitOfWork(dbContext, dapperContext);
    }
}
