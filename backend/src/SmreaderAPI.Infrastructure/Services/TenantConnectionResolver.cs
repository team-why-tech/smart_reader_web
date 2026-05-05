using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.Infrastructure.Services;

public class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly ITenantDatabaseRepository _tenantDatabaseRepository;
    private readonly ICacheService _cacheService;

    public TenantConnectionResolver(
        ITenantDatabaseRepository tenantDatabaseRepository,
        ICacheService cacheService)
    {
        _tenantDatabaseRepository = tenantDatabaseRepository;
        _cacheService = cacheService;
    }

    public async Task<string> ResolveConnectionStringAsync(int tenantId, string financialYear)
    {
        var cacheKey = $"tenant:{tenantId}:fy:{financialYear}";
        var cached = await _cacheService.GetAsync<string>(cacheKey);
        if (!string.IsNullOrWhiteSpace(cached))
            return cached;

        var tenantDb = await _tenantDatabaseRepository.GetByTenantAndYearAsync(tenantId, financialYear);
        if (tenantDb is null)
            throw new InvalidOperationException("Tenant database not found for the specified financial year.");

        await _cacheService.SetAsync(cacheKey, tenantDb.ConnectionString, TimeSpan.FromMinutes(30));
        return tenantDb.ConnectionString;
    }
}
