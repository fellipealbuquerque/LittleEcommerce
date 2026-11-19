using System.Text.Json;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using StackExchange.Redis;

namespace LittleEcommerce.Source.Infrastructure.Security;

public sealed class RolePermissionService(
    IConnectionMultiplexer redis,
    IRoleRepository roles,
    IPermissionRepository permissions,
    ILogger<RolePermissionService> logger) : IRolePermissionService
{
    public const string CachePrefix = "auth:role-permissions:";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(string roleName, CancellationToken ct = default)
    {
        var cached = await ReadCacheAsync(roleName);

        if (cached is not null)
            return cached;

        var codes = roleName == Roles.Admin
            ? (await permissions.ListAsync(ct)).Select(p => p.Code).ToList()
            : await roles.GetPermissionCodesAsync(roleName, ct);

        var granted = codes.ToHashSet(StringComparer.Ordinal);
        await WriteCacheAsync(roleName, granted);

        return granted;
    }

    public async Task InvalidateAsync(string roleName)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(CachePrefix + roleName);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Não foi possível invalidar o cache de permissões do papel {Role}", roleName);
        }
    }

    private async Task<IReadOnlySet<string>?> ReadCacheAsync(string roleName)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(CachePrefix + roleName);
            return value.HasValue
                ? JsonSerializer.Deserialize<string[]>(value.ToString())?.ToHashSet(StringComparer.Ordinal)
                : null;
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache de permissões indisponível; consultando o banco para o papel {Role}", roleName);
            return null;
        }
    }

    private async Task WriteCacheAsync(string roleName, IEnumerable<string> codes)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(CachePrefix + roleName, JsonSerializer.Serialize(codes), CacheTtl);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Não foi possível gravar o cache de permissões do papel {Role}", roleName);
        }
    }
}
