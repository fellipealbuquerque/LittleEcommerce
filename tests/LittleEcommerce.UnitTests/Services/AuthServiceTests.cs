using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Auth;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LittleEcommerce.UnitTests.Services;

public class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly ClientInfo Client = new("127.0.0.1", "xunit");

    private readonly InlineUnitOfWork _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<ITokenRevocationStore> _revocation = new();
    private readonly Mock<IOutbox> _outbox = new();
    private readonly Mock<IOutboxSignal> _signal = new();
    private readonly FakeTimeProvider _clock = new(Now);

    public AuthServiceTests()
    {
        _tokens.Setup(t => t.CreateAccessToken(It.IsAny<User>()))
            .Returns(new AccessToken("access-token", "jti-1", Now.UtcDateTime.AddMinutes(15)));
        _tokens.Setup(t => t.CreateRefreshToken())
            .Returns(new IssuedRefreshToken("refresh-token", "refresh-hash", Now.UtcDateTime.AddDays(7)));
        _tokens.Setup(t => t.HashRefreshToken(It.IsAny<string>()))
            .Returns((string value) => $"hash:{value}");
    }

    private AuthService CreateService() => new(
        _unitOfWork,
        _users.Object,
        _roles.Object,
        _refreshTokens.Object,
        _hasher.Object,
        _tokens.Object,
        _revocation.Object,
        _outbox.Object,
        _signal.Object,
        _clock,
        NullLogger<AuthService>.Instance);

    private RefreshToken StoredToken(User user, DateTime? revokedAt = null, string? reason = null, DateTime? expiresAt = null) => new()
    {
        UserId = user.Id,
        User = user,
        FamilyId = Guid.NewGuid(),
        TokenHash = "hash:antigo",
        CreatedAt = Now.UtcDateTime.AddDays(-1),
        ExpiresAt = expiresAt ?? Now.UtcDateTime.AddDays(6),
        RevokedAt = revokedAt,
        RevokedReason = reason
    };

    [Fact]
    public async Task RegisterAsync_ShouldThrowConflict_WhenNormalizedEmailExists()
    {
        _users.Setup(u => u.ExistsByEmailAsync("ana@loja.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().RegisterAsync(new RegisterRequest("Ana", "  Ana@Loja.COM ", "Senha12345"), Client));

        Assert.Equal(ErrorCodes.EmailAlreadyRegistered, ex.Code);
        _users.Verify(u => u.Add(It.IsAny<User>()), Times.Never);
        _signal.Verify(s => s.Notify(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateCustomerAndIssueTokens()
    {
        _roles.Setup(r => r.GetByNameAsync(Roles.Customer, It.IsAny<CancellationToken>())).ReturnsAsync(TestUsers.Customer);
        _hasher.Setup(h => h.Hash("Senha12345")).Returns("bcrypt-hash");
        User? created = null;
        _users.Setup(u => u.Add(It.IsAny<User>())).Callback<User>(u => created = u);
        RefreshToken? stored = null;
        _refreshTokens.Setup(r => r.Add(It.IsAny<RefreshToken>())).Callback<RefreshToken>(t => stored = t);

        var result = await CreateService().RegisterAsync(new RegisterRequest(" Ana ", "Ana@Loja.com", "Senha12345"), Client);

        Assert.NotNull(created);
        Assert.Equal("Ana", created.Name);
        Assert.Equal("ana@loja.com", created.NormalizedEmail);
        Assert.Equal("bcrypt-hash", created.PasswordHash);
        Assert.Equal(Roles.CustomerId, created.RoleId);
        Assert.NotNull(stored);
        Assert.Equal(created.Id, stored.UserId);
        Assert.Equal("refresh-hash", stored.TokenHash);
        Assert.NotEqual(Guid.Empty, stored.FamilyId);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(Roles.Customer, result.User.Role);
        Assert.Equal(1, _unitOfWork.Commits);
        _outbox.Verify(o => o.Enqueue(
            OutboxMessageTypes.WelcomeEmail,
            It.Is<WelcomeEmailPayload>(p => p.UserId == created.Id && p.Email == "Ana@Loja.com" && p.Name == "Ana"),
            OutboxAggregates.User,
            created.Id.ToString()), Times.Once);
        _signal.Verify(s => s.Notify(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldVerifyAgainstMissingHash_WhenUserDoesNotExist()
    {
        _users.Setup(u => u.GetByEmailAsync("ninguem@loja.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().LoginAsync(new LoginRequest("ninguem@loja.com", "Senha12345"), Client));

        Assert.Equal(ErrorCodes.InvalidCredentials, ex.Code);
        _hasher.Verify(h => h.Verify("Senha12345", null), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldRejectWrongPasswordWithSameCode()
    {
        var user = TestUsers.Create();
        _users.Setup(u => u.GetByEmailAsync(user.NormalizedEmail, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("errada", user.PasswordHash)).Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().LoginAsync(new LoginRequest(user.Email, "errada"), Client));

        Assert.Equal(ErrorCodes.InvalidCredentials, ex.Code);
    }

    [Fact]
    public async Task LoginAsync_ShouldRejectBlockedUser()
    {
        var user = TestUsers.Create(UserStatus.Blocked);
        _users.Setup(u => u.GetByEmailAsync(user.NormalizedEmail, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Senha12345", user.PasswordHash)).Returns(true);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService().LoginAsync(new LoginRequest(user.Email, "Senha12345"), Client));

        Assert.Equal(ErrorCodes.UserBlocked, ex.Code);
        _refreshTokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldRecordLastLoginAndOpenNewFamily()
    {
        var user = TestUsers.Create();
        _users.Setup(u => u.GetByEmailAsync(user.NormalizedEmail, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Senha12345", user.PasswordHash)).Returns(true);

        var result = await CreateService().LoginAsync(new LoginRequest(user.Email, "Senha12345"), Client);

        Assert.Equal(Now.UtcDateTime, user.LastLoginAt);
        Assert.Equal("access-token", result.AccessToken);
        _refreshTokens.Verify(r => r.Add(It.Is<RefreshToken>(t => t.UserId == user.Id && t.CreatedByIp == "127.0.0.1")), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRotateWithinSameFamily()
    {
        var user = TestUsers.Create();
        var current = StoredToken(user);
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:antigo-valor", It.IsAny<CancellationToken>())).ReturnsAsync(current);
        Guid replacedBy = Guid.Empty;
        _refreshTokens.Setup(r => r.TryRotateAsync(current.Id, It.IsAny<Guid>(), Now.UtcDateTime, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, DateTime, CancellationToken>((_, next, _, _) => replacedBy = next)
            .ReturnsAsync(true);
        RefreshToken? issued = null;
        _refreshTokens.Setup(r => r.Add(It.IsAny<RefreshToken>())).Callback<RefreshToken>(t => issued = t);

        var result = await CreateService().RefreshAsync(new RefreshTokenRequest("antigo-valor"), Client);

        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.NotNull(issued);
        Assert.Equal(current.FamilyId, issued.FamilyId);
        Assert.Equal(replacedBy, issued.Id);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRevokeFamilyAndSessions_WhenRotatedTokenIsReused()
    {
        var user = TestUsers.Create(sessionVersion: 1);
        var reused = StoredToken(user, Now.UtcDateTime.AddMinutes(-5), RevocationReasons.Rotated);
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:antigo-valor", It.IsAny<CancellationToken>())).ReturnsAsync(reused);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().RefreshAsync(new RefreshTokenRequest("antigo-valor"), Client));

        Assert.Equal(ErrorCodes.RefreshTokenReused, ex.Code);
        Assert.Equal(2, user.SessionVersion);
        Assert.Equal(1, _unitOfWork.Commits);
        _refreshTokens.Verify(r => r.RevokeFamilyAsync(reused.FamilyId, Now.UtcDateTime, RevocationReasons.ReuseDetected, It.IsAny<CancellationToken>()), Times.Once);
        _revocation.Verify(r => r.PublishSessionVersionAsync(user.Id, 2), Times.Once);
        _refreshTokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RevocationReasons.Logout)]
    [InlineData(RevocationReasons.LogoutAll)]
    [InlineData(RevocationReasons.PasswordChanged)]
    [InlineData(RevocationReasons.ReuseDetected)]
    public async Task RefreshAsync_ShouldOnlyReject_WhenTokenWasEndedDeliberately(string reason)
    {
        var user = TestUsers.Create();
        var ended = StoredToken(user, Now.UtcDateTime.AddMinutes(-5), reason);
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:antigo-valor", It.IsAny<CancellationToken>())).ReturnsAsync(ended);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().RefreshAsync(new RefreshTokenRequest("antigo-valor"), Client));

        Assert.Equal(ErrorCodes.InvalidRefreshToken, ex.Code);
        Assert.Equal(1, user.SessionVersion);
        _refreshTokens.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _revocation.Verify(r => r.PublishSessionVersionAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRejectExpiredToken()
    {
        var user = TestUsers.Create();
        var expired = StoredToken(user, expiresAt: Now.UtcDateTime.AddSeconds(-1));
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:antigo-valor", It.IsAny<CancellationToken>())).ReturnsAsync(expired);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().RefreshAsync(new RefreshTokenRequest("antigo-valor"), Client));

        Assert.Equal(ErrorCodes.RefreshTokenExpired, ex.Code);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReject_WhenConcurrentRotationWins()
    {
        var user = TestUsers.Create();
        var current = StoredToken(user);
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:antigo-valor", It.IsAny<CancellationToken>())).ReturnsAsync(current);
        _refreshTokens.Setup(r => r.TryRotateAsync(current.Id, It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateService().RefreshAsync(new RefreshTokenRequest("antigo-valor"), Client));

        Assert.Equal(ErrorCodes.InvalidRefreshToken, ex.Code);
        _refreshTokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_ShouldBlacklistAccessToken_AndIgnoreRefreshTokenOfAnotherUser()
    {
        var owner = TestUsers.Create();
        var foreign = StoredToken(owner);
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:de-outro", It.IsAny<CancellationToken>())).ReturnsAsync(foreign);
        var expiresAt = Now.UtcDateTime.AddMinutes(10);

        await CreateService().LogoutAsync(Guid.NewGuid(), "jti-9", expiresAt, new LogoutRequest("de-outro"));

        _revocation.Verify(r => r.RevokeAsync("jti-9", expiresAt), Times.Once);
        _refreshTokens.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeOwnRefreshFamily()
    {
        var user = TestUsers.Create();
        var own = StoredToken(user);
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:meu", It.IsAny<CancellationToken>())).ReturnsAsync(own);

        await CreateService().LogoutAsync(user.Id, "jti-1", Now.UtcDateTime.AddMinutes(10), new LogoutRequest("meu"));

        _refreshTokens.Verify(r => r.RevokeFamilyAsync(own.FamilyId, Now.UtcDateTime, RevocationReasons.Logout, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAllAsync_ShouldBumpSessionVersionAndRevokeEveryRefreshToken()
    {
        var user = TestUsers.Create(sessionVersion: 4);
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await CreateService().LogoutAllAsync(user.Id);

        Assert.Equal(5, user.SessionVersion);
        _refreshTokens.Verify(r => r.RevokeAllForUserAsync(user.Id, Now.UtcDateTime, RevocationReasons.LogoutAll, It.IsAny<CancellationToken>()), Times.Once);
        _revocation.Verify(r => r.PublishSessionVersionAsync(user.Id, 5), Times.Once);
    }
}
