using System.Security.Claims;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Moq;

namespace LittleEcommerce.UnitTests.Security;

public class PermissionAuthorizationHandlerTests
{
    private readonly Mock<IRolePermissionService> _permissions = new();

    private async Task<bool> AuthorizeAsync(string? role, string permission)
    {
        var claims = role is null ? [] : new[] { new Claim(AppClaims.Role, role) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var requirement = new PermissionRequirement(permission);
        var context = new AuthorizationHandlerContext([requirement], user, null);

        await new PermissionAuthorizationHandler(_permissions.Object).HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenRoleHasPermission()
    {
        _permissions.Setup(p => p.GetPermissionsAsync(Roles.Manager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { Permissions.UsersRead });

        Assert.True(await AuthorizeAsync(Roles.Manager, Permissions.UsersRead));
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRoleLacksPermission()
    {
        _permissions.Setup(p => p.GetPermissionsAsync(Roles.Manager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { Permissions.UsersRead });

        Assert.False(await AuthorizeAsync(Roles.Manager, Permissions.UsersManage));
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTokenHasNoRole()
    {
        Assert.False(await AuthorizeAsync(null, Permissions.UsersRead));
        _permissions.Verify(p => p.GetPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
