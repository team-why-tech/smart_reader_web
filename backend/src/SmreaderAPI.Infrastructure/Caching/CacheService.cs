using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.Infrastructure.Caching;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly TimeSpan _l1Expiry;
    private readonly TimeSpan _l2Expiry;
    private static readonly HashSet<string> _keys = new();
    private static readonly object _lock = new();

    public CacheService(IMemoryCache memoryCache, IDistributedCache distributedCache, IConfiguration configuration)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _l1Expiry = TimeSpan.FromMinutes(int.Parse(configuration["Cache:L1ExpiryMinutes"] ?? "5"));
        _l2Expiry = TimeSpan.FromMinutes(int.Parse(configuration["Cache:L2ExpiryMinutes"] ?? "30"));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        // L1: Memory cache
        if (_memoryCache.TryGetValue(key, out T? value))
            return value;

        // L2: Redis
        var bytes = await _distributedCache.GetStringAsync(key);
        if (bytes is not null)
        {
            value = JsonSerializer.Deserialize<T>(bytes);
            if (value is not null)
            {
                // Promote to L1
                _memoryCache.Set(key, value, _l1Expiry);
            }
            return value;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var l1Ttl = expiry ?? _l1Expiry;
        var l2Ttl = expiry ?? _l2Expiry;

        // L1
        _memoryCache.Set(key, value, l1Ttl);

        // L2
        var json = JsonSerializer.Serialize(value);
        await _distributedCache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = l2Ttl
        });

        TrackKey(key);
    }

    public async Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key);
        UntrackKey(key);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        List<string> matchingKeys;
        lock (_lock)
        {
            matchingKeys = _keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (var key in matchingKeys)
        {
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key);
            UntrackKey(key);
        }
    }

    private static void TrackKey(string key)
    {
        lock (_lock) { _keys.Add(key); }
    }

    private static void UntrackKey(string key)
    {
        lock (_lock) { _keys.Remove(key); }
    }
}
