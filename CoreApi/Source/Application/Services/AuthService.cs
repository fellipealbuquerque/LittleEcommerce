using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Mappers;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Auth;
using LittleEcommerce.Source.Shared.DTOs.Results.Auth;
using LittleEcommerce.Source.Shared.Helpers;

namespace LittleEcommerce.Source.Application.Services;

public class AuthService(
    IUnitOfWork unitOfWork,
    IUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher hasher,
    ITokenService tokens,
    ITokenRevocationStore revocation,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    private const int UserAgentMaxLength = 300;
    private const string TokenType = "Bearer";

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, ClientInfo client, CancellationToken ct = default)
    {
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);

        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            if (await users.ExistsByEmailAsync(normalizedEmail, ct))
                throw new ConflictException("E-mail já cadastrado", ErrorCodes.EmailAlreadyRegistered);

            var role = await roles.GetByNameAsync(Roles.Customer, ct)
                       ?? throw new InvalidOperationException($"Papel {Roles.Customer} não encontrado");

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = hasher.Hash(request.Password),
                RoleId = role.Id,
                Role = role,
                CreatedAt = Now
            };

            users.Add(user);

            return Issue(user, Guid.CreateVersion7(), client);
        }, ct);

        logger.LogInformation("Usuário {UserId} cadastrado", result.User.Id);

        return result;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, ClientInfo client, CancellationToken ct = default)
    {
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);

        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            var user = await users.GetByEmailAsync(normalizedEmail, ct);

            if (!hasher.Verify(request.Password, user?.PasswordHash) || user is null)
            {
                logger.LogWarning("Falha de login para {Email}", normalizedEmail);
                throw new UnauthorizedException("E-mail ou senha inválidos", ErrorCodes.InvalidCredentials);
            }

            if (user.Status != UserStatus.Active)
                throw new ForbiddenException("Usuário bloqueado", ErrorCodes.UserBlocked);

            user.LastLoginAt = Now;

            return Issue(user, Guid.CreateVersion7(), client);
        }, ct);

        logger.LogInformation("Login do usuário {UserId}", result.User.Id);

        return result;
    }

    public async Task<AuthResult> RefreshAsync(RefreshTokenRequest request, ClientInfo client, CancellationToken ct = default)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);

        var outcome = await unitOfWork.ExecuteAsync(async () =>
        {
            var current = await refreshTokens.GetByHashAsync(hash, ct);

            if (current is null)
                return RefreshOutcome.Fail(new UnauthorizedException("Refresh token inválido", ErrorCodes.InvalidRefreshToken));

            if (current.RevokedAt is not null && current.RevokedReason == RevocationReasons.Rotated)
            {
                await refreshTokens.RevokeFamilyAsync(current.FamilyId, Now, RevocationReasons.ReuseDetected, ct);
                current.User.InvalidateSessions();
                return RefreshOutcome.Reused(current.User);
            }

            if (current.RevokedAt is not null)
                return RefreshOutcome.Fail(new UnauthorizedException("Sessão encerrada. Entre novamente.", ErrorCodes.InvalidRefreshToken));

            if (current.ExpiresAt <= Now)
                return RefreshOutcome.Fail(new UnauthorizedException("Refresh token expirado", ErrorCodes.RefreshTokenExpired));

            if (current.User.Status != UserStatus.Active)
                return RefreshOutcome.Fail(new ForbiddenException("Usuário bloqueado", ErrorCodes.UserBlocked));

            var nextId = Guid.CreateVersion7();

            if (!await refreshTokens.TryRotateAsync(current.Id, nextId, Now, ct))
                return RefreshOutcome.Fail(new UnauthorizedException("Refresh token inválido", ErrorCodes.InvalidRefreshToken));

            return RefreshOutcome.Success(Issue(current.User, current.FamilyId, client, nextId));
        }, ct);

        if (outcome.ReusedBy is { } reusedBy)
        {
            await revocation.PublishSessionVersionAsync(reusedBy.Id, reusedBy.SessionVersion);
            logger.LogWarning("Reuso de refresh token detectado para o usuário {UserId}; todas as sessões foram revogadas", reusedBy.Id);
        }

        return outcome.Result ?? throw outcome.Error!;
    }

    public async Task LogoutAsync(Guid userId, string tokenId, DateTime tokenExpiresAt, LogoutRequest request, CancellationToken ct = default)
    {
        await revocation.RevokeAsync(tokenId, tokenExpiresAt);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var token = await refreshTokens.GetByHashAsync(tokens.HashRefreshToken(request.RefreshToken), ct);

            if (token is not null && token.UserId == userId)
                await refreshTokens.RevokeFamilyAsync(token.FamilyId, Now, RevocationReasons.Logout, ct);
        }

        logger.LogInformation("Logout do usuário {UserId}", userId);
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken ct = default)
    {
        var version = await unitOfWork.ExecuteAsync(async () =>
        {
            var user = await users.GetByIdAsync(userId, ct)
                       ?? throw new NotFoundException("Usuário não encontrado");

            user.InvalidateSessions();
            await refreshTokens.RevokeAllForUserAsync(userId, Now, RevocationReasons.LogoutAll, ct);

            return user.SessionVersion;
        }, ct);

        await revocation.PublishSessionVersionAsync(userId, version);

        logger.LogInformation("Todas as sessões do usuário {UserId} foram encerradas", userId);
    }

    private AuthResult Issue(User user, Guid familyId, ClientInfo client, Guid? refreshTokenId = null)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();

        refreshTokens.Add(new RefreshToken
        {
            Id = refreshTokenId ?? Guid.CreateVersion7(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = refresh.Hash,
            CreatedAt = Now,
            ExpiresAt = refresh.ExpiresAt,
            CreatedByIp = client.IpAddress,
            UserAgent = client.UserAgent is { Length: > UserAgentMaxLength } agent ? agent[..UserAgentMaxLength] : client.UserAgent
        });

        return new AuthResult(access.Value, access.ExpiresAt, refresh.Value, refresh.ExpiresAt, TokenType, user.ToResult());
    }

    private sealed record RefreshOutcome(AuthResult? Result, AppException? Error, User? ReusedBy)
    {
        public static RefreshOutcome Success(AuthResult result) => new(result, null, null);

        public static RefreshOutcome Fail(AppException error) => new(null, error, null);

        public static RefreshOutcome Reused(User user) => new(
            null,
            new UnauthorizedException(
                "Refresh token já utilizado. Por segurança, todas as sessões foram encerradas.",
                ErrorCodes.RefreshTokenReused),
            user);
    }
}
