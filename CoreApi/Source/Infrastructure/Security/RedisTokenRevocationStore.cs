using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LittleEcommerce.Source.Infrastructure.Security;

public sealed class RedisTokenRevocationStore(
    IConnectionMultiplexer redis,
    IUserRepository users,
    IOptions<JwtOptions> options,
    TimeProvider clock) : ITokenRevocationStore
{
    public const string BlacklistPrefix = "auth:blacklist:";
    public const string SessionVersionPrefix = "auth:session-version:";

    private IDatabase Database => redis.GetDatabase();

    private TimeSpan SessionVersionTtl => TimeSpan.FromMinutes(Math.Max(1, options.Value.AccessTokenMinutes));

    public async Task RevokeAsync(string tokenId, DateTime expiresAt)
    {
        var remaining = expiresAt - clock.GetUtcNow().UtcDateTime;

        if (remaining <= TimeSpan.Zero)
            return;

        await Database.StringSetAsync(BlacklistPrefix + tokenId, 1, remaining);
    }

    public Task<bool> IsRevokedAsync(string tokenId) => Database.KeyExistsAsync(BlacklistPrefix + tokenId);

    public async Task<int?> GetSessionVersionAsync(Guid userId, CancellationToken ct)
    {
        var cached = await Database.StringGetAsync(SessionVersionPrefix + userId);

        if (cached.HasValue && cached.TryParse(out int version))
            return version;

        var current = await users.GetActiveSessionVersionAsync(userId, ct);

        if (current is not null)
            await PublishSessionVersionAsync(userId, current.Value);

        return current;
    }

    public Task PublishSessionVersionAsync(Guid userId, int version) =>
        Database.StringSetAsync(SessionVersionPrefix + userId, version, SessionVersionTtl);
}
