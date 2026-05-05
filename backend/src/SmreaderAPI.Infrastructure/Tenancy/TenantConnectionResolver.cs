using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmreaderAPI.Application.Interfaces;
using SmreaderAPI.Domain.Interfaces;

namespace SmreaderAPI.Infrastructure.Tenancy;

/// <summary>
/// Resolves tenant database connection strings with caching strategy
/// Cache key format: tenant:{tenantId}:fy:{financialYear}
/// </summary>
public class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly IMasterUnitOfWork _masterUnitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TenantConnectionResolver> _logger;
    private readonly TimeSpan _cacheTtl;

    public TenantConnectionResolver(
        IMasterUnitOfWork masterUnitOfWork,
        ICacheService cacheService,
        IConfiguration configuration,
        ILogger<TenantConnectionResolver> logger)
    {
        _masterUnitOfWork = masterUnitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        
        var ttlMinutes = int.Parse(configuration["Cache:TenantConnectionTtlMinutes"] ?? "30");
        _cacheTtl = TimeSpan.FromMinutes(ttlMinutes);
    }

    public async Task<string> ResolveConnectionStringAsync(int tenantId, string financialYear)
    {
        var cacheKey = $"tenant:{tenantId}:fy:{financialYear}";

        // Try cache first
        var cached = await _cacheService.GetAsync<string>(cacheKey);
        if (cached is not null)
        {
            _logger.LogDebug("Resolved tenant connection from cache: {TenantId}, {FY}", tenantId, financialYear);
            return cached;
        }

        // Fallback to master DB
        _logger.LogDebug("Cache miss, resolving tenant connection from master DB: {TenantId}, {FY}", tenantId, financialYear);
        
        var tenantDb = await _masterUnitOfWork.TenantDatabases.GetByTenantAndFyAsync(tenantId, financialYear);
        
        if (tenantDb is null)
        {
            throw new InvalidOperationException(
                $"No database configuration found for tenant {tenantId} and financial year {financialYear}");
        }

        // Cache the connection string
        await _cacheService.SetAsync(cacheKey, tenantDb.ConnectionString, _cacheTtl);
        
        _logger.LogInformation("Resolved and cached tenant connection: {TenantId}, {FY}", tenantId, financialYear);
        
        return tenantDb.ConnectionString;
    }

    public async Task InvalidateCacheAsync(int tenantId, string financialYear)
    {
        var cacheKey = $"tenant:{tenantId}:fy:{financialYear}";
        await _cacheService.RemoveAsync(cacheKey);
        _logger.LogInformation("Invalidated tenant connection cache: {TenantId}, {FY}", tenantId, financialYear);
    }
}
