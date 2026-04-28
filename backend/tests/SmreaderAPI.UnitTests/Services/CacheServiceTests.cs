using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using SmreaderAPI.Infrastructure.Caching;

namespace SmreaderAPI.UnitTests.Services;

public class CacheServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly CacheService _sut;

    public CacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _distributedCacheMock = new Mock<IDistributedCache>();

        var configData = new Dictionary<string, string?>
        {
            { "Cache:L1ExpiryMinutes", "5" },
            { "Cache:L2ExpiryMinutes", "30" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _sut = new CacheService(_memoryCache, _distributedCacheMock.Object, configuration);
    }

    [Fact]
    public async Task GetAsync_L1Hit_ReturnsFromMemory()
    {
        // Arrange
        _memoryCache.Set("test-key", "test-value");

        // Act
        var result = await _sut.GetAsync<string>("test-key");

        // Assert
        result.Should().Be("test-value");
        _distributedCacheMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_L1Miss_L2Hit_PromotesToL1()
    {
        // Arrange
        var json = JsonSerializer.Serialize("redis-value");
        _distributedCacheMock.Setup(x => x.GetAsync("test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _sut.GetAsync<string>("test-key");

        // Assert
        result.Should().Be("redis-value");
        _memoryCache.TryGetValue("test-key", out string? cached).Should().BeTrue();
        cached.Should().Be("redis-value");
    }

    [Fact]
    public async Task GetAsync_BothMiss_ReturnsDefault()
    {
        // Arrange
        _distributedCacheMock.Setup(x => x.GetAsync("missing-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _sut.GetAsync<string>("missing-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WritesToBothLayers()
    {
        // Act
        await _sut.SetAsync("key", "value");

        // Assert
        _memoryCache.TryGetValue("key", out string? cached).Should().BeTrue();
        cached.Should().Be("value");
        _distributedCacheMock.Verify(x => x.SetAsync("key", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromBothLayers()
    {
        // Arrange
        _memoryCache.Set("key", "value");

        // Act
        await _sut.RemoveAsync("key");

        // Assert
        _memoryCache.TryGetValue("key", out _).Should().BeFalse();
        _distributedCacheMock.Verify(x => x.RemoveAsync("key", It.IsAny<CancellationToken>()), Times.Once);
    }
}
