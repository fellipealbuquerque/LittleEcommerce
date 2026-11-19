using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Shared.DTOs.Requests.Auth;
using LittleEcommerce.Source.Shared.DTOs.Results.Auth;

namespace LittleEcommerce.Source.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, ClientInfo client, CancellationToken ct = default);

    Task<AuthResult> LoginAsync(LoginRequest request, ClientInfo client, CancellationToken ct = default);

    Task<AuthResult> RefreshAsync(RefreshTokenRequest request, ClientInfo client, CancellationToken ct = default);

    Task LogoutAsync(Guid userId, string tokenId, DateTime tokenExpiresAt, LogoutRequest request, CancellationToken ct = default);

    Task LogoutAllAsync(Guid userId, CancellationToken ct = default);
}
