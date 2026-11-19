using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LittleEcommerce.UnitTests.Services;

public class AdminUserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Role Admin = new() { Id = Roles.AdminId, Name = Roles.Admin, IsSystem = true };
    private static readonly Role Manager = new() { Id = Roles.ManagerId, Name = Roles.Manager, IsSystem = true };

    private readonly InlineUnitOfWork _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<ITokenRevocationStore> _revocation = new();
    private readonly Guid _actorId = Guid.NewGuid();

    private AdminUserService CreateService() => new(
        _unitOfWork,
        _users.Object,
        _roles.Object,
        _refreshTokens.Object,
        _revocation.Object,
        new FakeTimeProvider(Now),
        NullLogger<AdminUserService>.Instance);

    private User Existing(Role? role = null)
    {
        var user = TestUsers.Create();
        if (role is not null)
        {
            user.Role = role;
            user.RoleId = role.Id;
        }

        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task ChangeRoleAsync_ShouldRefuseOwnAccount()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().ChangeRoleAsync(_actorId, _actorId, new ChangeUserRoleRequest(Roles.Customer)));

        Assert.Equal(ErrorCodes.CannotChangeOwnAccount, ex.Code);
    }

    [Fact]
    public async Task ChangeRoleAsync_ShouldRejectUnknownRole()
    {
        var user = Existing();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().ChangeRoleAsync(_actorId, user.Id, new ChangeUserRoleRequest("Inexistente")));

        Assert.True(ex.Errors.ContainsKey("role"));
    }

    [Fact]
    public async Task ChangeRoleAsync_ShouldSwitchRoleAndEndSessions()
    {
        var user = Existing();
        _roles.Setup(r => r.GetByNameAsync(Roles.Manager, It.IsAny<CancellationToken>())).ReturnsAsync(Manager);

        var result = await CreateService().ChangeRoleAsync(_actorId, user.Id, new ChangeUserRoleRequest(Roles.Manager));

        Assert.Equal(Roles.Manager, result.Role);
        Assert.Equal(2, user.SessionVersion);
        _refreshTokens.Verify(r => r.RevokeAllForUserAsync(user.Id, Now.UtcDateTime, RevocationReasons.RoleChanged, It.IsAny<CancellationToken>()), Times.Once);
        _revocation.Verify(r => r.PublishSessionVersionAsync(user.Id, 2), Times.Once);
    }

    [Fact]
    public async Task BlockAsync_ShouldRefuseLastActiveAdmin()
    {
        var admin = Existing(Admin);
        _users.Setup(u => u.CountActiveInRoleAsync(Roles.AdminId, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => CreateService().BlockAsync(_actorId, admin.Id));

        Assert.Equal(ErrorCodes.LastAdmin, ex.Code);
        Assert.Equal(UserStatus.Active, admin.Status);
    }

    [Fact]
    public async Task BlockAsync_ShouldAllowAdmin_WhenAnotherAdminRemains()
    {
        var admin = Existing(Admin);
        _users.Setup(u => u.CountActiveInRoleAsync(Roles.AdminId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateService().BlockAsync(_actorId, admin.Id);

        Assert.Equal(UserStatus.Blocked, result.Status);
    }

    [Fact]
    public async Task BlockAsync_ShouldBlockAndEndSessions()
    {
        var user = Existing();

        var result = await CreateService().BlockAsync(_actorId, user.Id);

        Assert.Equal(UserStatus.Blocked, result.Status);
        Assert.Equal(2, user.SessionVersion);
        _refreshTokens.Verify(r => r.RevokeAllForUserAsync(user.Id, Now.UtcDateTime, RevocationReasons.Blocked, It.IsAny<CancellationToken>()), Times.Once);
        _revocation.Verify(r => r.PublishSessionVersionAsync(user.Id, 2), Times.Once);
    }

    [Fact]
    public async Task UnblockAsync_ShouldReactivateWithoutTouchingSessions()
    {
        var user = Existing();
        user.Status = UserStatus.Blocked;

        var result = await CreateService().UnblockAsync(user.Id);

        Assert.Equal(UserStatus.Active, result.Status);
        Assert.Equal(1, user.SessionVersion);
        Assert.Equal(1, _unitOfWork.Commits);
    }

    [Fact]
    public async Task RevokeSessionsAsync_ShouldBumpVersionAndRevokeRefreshTokens()
    {
        var user = Existing();

        await CreateService().RevokeSessionsAsync(user.Id);

        Assert.Equal(2, user.SessionVersion);
        _refreshTokens.Verify(r => r.RevokeAllForUserAsync(user.Id, Now.UtcDateTime, RevocationReasons.AdminRevoked, It.IsAny<CancellationToken>()), Times.Once);
    }
}
