using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SmreaderAPI.Infrastructure.Caching;

/// <summary>
/// Caches resolved tenant connection strings in IMemoryCache to avoid
/// hitting the Master DB on every request. Singleton lifetime.
/// </summary>
public class TenantConnectionStringCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantConnectionStringCache> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public TenantConnectionStringCache(IMemoryCache cache, ILogger<TenantConnectionStringCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string? Get(int tenantId)
    {
        if (_cache.TryGetValue(GetKey(tenantId), out string? connStr))
        {
            _logger.LogDebug("Tenant connection string cache HIT for tenant {TenantId}", tenantId);
            return connStr;
        }

        _logger.LogDebug("Tenant connection string cache MISS for tenant {TenantId}", tenantId);
        return null;
    }

    public void Set(int tenantId, string connectionString)
    {
        _cache.Set(GetKey(tenantId), connectionString, CacheDuration);
        _logger.LogDebug("Cached connection string for tenant {TenantId} (TTL: {Minutes}m)", tenantId, CacheDuration.TotalMinutes);
    }

    public void Evict(int tenantId)
    {
        _cache.Remove(GetKey(tenantId));
        _logger.LogInformation("Evicted cached connection string for tenant {TenantId}", tenantId);
    }

    private static string GetKey(int tenantId) => $"tenant_conn:{tenantId}";
}
