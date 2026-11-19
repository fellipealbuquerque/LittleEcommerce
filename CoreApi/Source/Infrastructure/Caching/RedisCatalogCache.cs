using System.Text.Json;
using LittleEcommerce.Source.Application.Interfaces.Caching;
using LittleEcommerce.Source.Shared.Options;
using LittleEcommerce.Source.Shared.Serialization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LittleEcommerce.Source.Infrastructure.Caching;

public sealed class RedisCatalogCache(
    IConnectionMultiplexer redis,
    IOptions<CatalogCacheOptions> options,
    ILogger<RedisCatalogCache> logger) : ICatalogCache
{
    public const string VersionKey = "catalog:version";

    private TimeSpan Ttl => TimeSpan.FromSeconds(Math.Max(1, options.Value.TtlSeconds));

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
    {
        if (!options.Value.Enabled)
            return await factory();

        string fullKey;

        try
        {
            var database = redis.GetDatabase();
            var version = await database.StringGetAsync(VersionKey);
            fullKey = $"catalog:v{(version.HasValue ? version.ToString() : "0")}:{key}";

            var cached = await database.StringGetAsync(fullKey);

            if (cached.HasValue)
                return JsonSerializer.Deserialize<T>(cached.ToString(), JsonDefaults.Options)!;
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache do catálogo indisponível; lendo direto do banco ({Key})", key);
            return await factory();
        }

        var value = await factory();

        try
        {
            await redis.GetDatabase().StringSetAsync(fullKey, JsonSerializer.Serialize(value, JsonDefaults.Options), Ttl);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Não foi possível gravar {Key} no cache do catálogo", key);
        }

        return value;
    }

    public async Task InvalidateAsync()
    {
        if (!options.Value.Enabled)
            return;

        try
        {
            await redis.GetDatabase().StringIncrementAsync(VersionKey);
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Não foi possível invalidar o cache do catálogo; as entradas antigas expiram em {Ttl}", Ttl);
        }
    }
}
