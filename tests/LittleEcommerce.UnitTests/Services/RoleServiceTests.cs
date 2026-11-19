using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LittleEcommerce.UnitTests.Services;

public class RoleServiceTests
{
    private readonly InlineUnitOfWork _unitOfWork = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IPermissionRepository> _permissions = new();
    private readonly Mock<IRolePermissionService> _cache = new();

    private static readonly Permission UsersRead = new() { Code = Permissions.UsersRead, Group = "Usuários", Description = "ler" };
    private static readonly Permission AuditRead = new() { Code = Permissions.AuditRead, Group = "Auditoria", Description = "ler" };

    public RoleServiceTests()
    {
        _roles.Setup(r => r.CountUsersByRoleAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());
        _permissions.Setup(p => p.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> codes, CancellationToken _) =>
                new[] { UsersRead, AuditRead }.Where(p => codes.Contains(p.Code)).ToList());
    }

    private RoleService CreateService() => new(_unitOfWork, _roles.Object, _permissions.Object, _cache.Object, NullLogger<RoleService>.Instance);

    private static Role CustomRole(params Permission[] granted)
    {
        var role = new Role { Name = "Auditor" };
        foreach (var permission in granted)
            role.RolePermissions.Add(new RolePermission { Role = role, RoleId = role.Id, Permission = permission, PermissionId = permission.Id });
        return role;
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateName()
    {
        _roles.Setup(r => r.ExistsByNameAsync("Auditor", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService().CreateAsync(new CreateRoleRequest(" Auditor ", null, [Permissions.AuditRead])));

        Assert.Equal(ErrorCodes.RoleAlreadyExists, ex.Code);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUnknownPermissions()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService().CreateAsync(new CreateRoleRequest("Auditor", null, [Permissions.AuditRead, "nao.existe"])));

        Assert.Contains("nao.existe", ex.Errors["permissions"][0]);
        _roles.Verify(r => r.Add(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistRoleWithPermissions()
    {
        Role? created = null;
        _roles.Setup(r => r.Add(It.IsAny<Role>())).Callback<Role>(r => created = r);

        var result = await CreateService().CreateAsync(new CreateRoleRequest("Auditor", "Lê auditoria", [Permissions.AuditRead]));

        Assert.NotNull(created);
        Assert.False(created.IsSystem);
        Assert.Equal([Permissions.AuditRead], result.Permissions);
        Assert.Equal(1, _unitOfWork.Commits);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_ShouldRefuseAdminRole()
    {
        var admin = new Role { Id = Roles.AdminId, Name = Roles.Admin, IsSystem = true };
        _roles.Setup(r => r.GetWithPermissionsAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            CreateService().UpdatePermissionsAsync(admin.Id, new UpdateRolePermissionsRequest([])));

        Assert.Equal(ErrorCodes.AdminRoleLocked, ex.Code);
    }

    [Fact]
    public async Task UpdatePermissionsAsync_ShouldReplaceSetAndInvalidateCache()
    {
        var role = CustomRole(UsersRead);
        _roles.Setup(r => r.GetWithPermissionsAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        var result = await CreateService().UpdatePermissionsAsync(role.Id, new UpdateRolePermissionsRequest([Permissions.AuditRead]));

        Assert.Equal([Permissions.AuditRead], result.Permissions);
        Assert.Single(role.RolePermissions);
        _cache.Verify(c => c.InvalidateAsync("Auditor"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRefuseSystemRole()
    {
        var customer = new Role { Id = Roles.CustomerId, Name = Roles.Customer, IsSystem = true };
        _roles.Setup(r => r.GetWithPermissionsAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var ex = await Assert.ThrowsAsync<DomainException>(() => CreateService().DeleteAsync(customer.Id));

        Assert.Equal(ErrorCodes.SystemRoleLocked, ex.Code);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRefuseRoleWithUsers()
    {
        var role = CustomRole();
        _roles.Setup(r => r.GetWithPermissionsAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _roles.Setup(r => r.HasUsersAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().DeleteAsync(role.Id));

        Assert.Equal(ErrorCodes.RoleInUse, ex.Code);
        _roles.Verify(r => r.Remove(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAndInvalidate()
    {
        var role = CustomRole(AuditRead);
        _roles.Setup(r => r.GetWithPermissionsAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        await CreateService().DeleteAsync(role.Id);

        _roles.Verify(r => r.Remove(role), Times.Once);
        _cache.Verify(c => c.InvalidateAsync("Auditor"), Times.Once);
    }
}
