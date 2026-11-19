using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Users;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LittleEcommerce.UnitTests.Services;

public class UserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly InlineUnitOfWork _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenRevocationStore> _revocation = new();

    private UserService CreateService() => new(
        _unitOfWork,
        _users.Object,
        _refreshTokens.Object,
        _hasher.Object,
        _revocation.Object,
        new FakeTimeProvider(Now),
        NullLogger<UserService>.Instance);

    [Fact]
    public async Task GetAsync_ShouldThrowNotFound_WhenUserIsMissing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldTrimNameAndSave()
    {
        var user = TestUsers.Create();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateService().UpdateProfileAsync(user.Id, new UpdateProfileRequest("  Ana Maria  "));

        Assert.Equal("Ana Maria", result.Name);
        Assert.Equal(1, _unitOfWork.Commits);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldRejectWrongCurrentPassword()
    {
        var user = TestUsers.Create();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("errada", user.PasswordHash)).Returns(false);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().ChangePasswordAsync(user.Id, new ChangePasswordRequest("errada", "NovaSenha123")));

        Assert.True(ex.Errors.ContainsKey("currentPassword"));
        _revocation.Verify(r => r.PublishSessionVersionAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldRejectSamePassword()
    {
        var user = TestUsers.Create();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Senha12345", user.PasswordHash)).Returns(true);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().ChangePasswordAsync(user.Id, new ChangePasswordRequest("Senha12345", "Senha12345")));

        Assert.True(ex.Errors.ContainsKey("newPassword"));
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldStoreNewHashAndEndEverySession()
    {
        var user = TestUsers.Create(sessionVersion: 2);
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Senha12345", "hash-atual")).Returns(true);
        _hasher.Setup(h => h.Verify("NovaSenha123", "hash-atual")).Returns(false);
        _hasher.Setup(h => h.Hash("NovaSenha123")).Returns("hash-novo");

        await CreateService().ChangePasswordAsync(user.Id, new ChangePasswordRequest("Senha12345", "NovaSenha123"));

        Assert.Equal("hash-novo", user.PasswordHash);
        Assert.Equal(3, user.SessionVersion);
        _refreshTokens.Verify(r => r.RevokeAllForUserAsync(user.Id, Now.UtcDateTime, RevocationReasons.PasswordChanged, It.IsAny<CancellationToken>()), Times.Once);
        _revocation.Verify(r => r.PublishSessionVersionAsync(user.Id, 3), Times.Once);
    }
}
