namespace LittleEcommerce.Source.Application.Interfaces.Security;

public interface ITokenRevocationStore
{
    Task RevokeAsync(string tokenId, DateTime expiresAt);

    Task<bool> IsRevokedAsync(string tokenId);

    Task<int?> GetSessionVersionAsync(Guid userId, CancellationToken ct);

    Task PublishSessionVersionAsync(Guid userId, int version);
}
