using LittleEcommerce.Source.Infrastructure.Caching;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace LittleEcommerce.UnitTests.Catalog;

public class RedisCatalogCacheTests
{
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _database = new();

    public RedisCatalogCacheTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(_database.Object);
    }

    private RedisCatalogCache CreateCache(bool enabled = true) =>
        new(_redis.Object, MsOptions.Create(new CatalogCacheOptions { Enabled = enabled, TtlSeconds = 60 }), NullLogger<RedisCatalogCache>.Instance);

    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnCachedValueForCurrentVersion()
    {
        _database.Setup(d => d.StringGetAsync((RedisKey)RedisCatalogCache.VersionKey, It.IsAny<CommandFlags>())).ReturnsAsync((RedisValue)"3");
        _database.Setup(d => d.StringGetAsync((RedisKey)"catalog:v3:chave", It.IsAny<CommandFlags>())).ReturnsAsync((RedisValue)"42");
        var calls = 0;

        var value = await CreateCache().GetOrCreateAsync("chave", () => Task.FromResult(++calls));

        Assert.Equal(42, value);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldFallBackToFactory_WhenRedisIsDown()
    {
        _database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "fora"));

        var value = await CreateCache().GetOrCreateAsync("chave", () => Task.FromResult(7));

        Assert.Equal(7, value);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldSkipRedis_WhenDisabled()
    {
        var value = await CreateCache(enabled: false).GetOrCreateAsync("chave", () => Task.FromResult(5));

        Assert.Equal(5, value);
        _redis.Verify(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()), Times.Never);
    }

    [Fact]
    public async Task InvalidateAsync_ShouldBumpVersion()
    {
        await CreateCache().InvalidateAsync();

        _database.Verify(d => d.StringIncrementAsync((RedisKey)RedisCatalogCache.VersionKey, 1L, It.IsAny<CommandFlags>()), Times.Once);
    }
}
