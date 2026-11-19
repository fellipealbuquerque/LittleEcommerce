using System.Security.Claims;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Common.Constants;
using Microsoft.AspNetCore.Authorization;

namespace LittleEcommerce.Source.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(IRolePermissionService permissions)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var role = context.User.FindFirstValue(AppClaims.Role);

        if (role is null)
            return;

        var granted = await permissions.GetPermissionsAsync(role);

        if (granted.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
