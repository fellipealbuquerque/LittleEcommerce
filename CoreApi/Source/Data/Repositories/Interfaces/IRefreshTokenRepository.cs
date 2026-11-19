using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);

    Task<bool> TryRotateAsync(Guid id, Guid replacedById, DateTime now, CancellationToken ct);

    Task<int> RevokeFamilyAsync(Guid familyId, DateTime now, string reason, CancellationToken ct);

    Task<int> RevokeAllForUserAsync(Guid userId, DateTime now, string reason, CancellationToken ct);

    Task<int> DeleteExpiredAsync(DateTime expiredBefore, CancellationToken ct);
}
