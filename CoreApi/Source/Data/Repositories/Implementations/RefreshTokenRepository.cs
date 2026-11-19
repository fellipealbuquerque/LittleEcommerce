using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<bool> TryRotateAsync(Guid id, Guid replacedById, DateTime now, CancellationToken ct)
    {
        var updated = await context.RefreshTokens
            .Where(t => t.Id == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, RevocationReasons.Rotated)
                .SetProperty(t => t.ReplacedById, replacedById), ct);

        return updated == 1;
    }

    public Task<int> RevokeFamilyAsync(Guid familyId, DateTime now, string reason, CancellationToken ct) =>
        context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), ct);

    public Task<int> RevokeAllForUserAsync(Guid userId, DateTime now, string reason, CancellationToken ct) =>
        context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), ct);

    public Task<int> DeleteExpiredAsync(DateTime expiredBefore, CancellationToken ct) =>
        context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.ExpiresAt < expiredBefore)
            .ExecuteDeleteAsync(ct);
}
