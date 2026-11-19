using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.Source.Shared.DTOs.Results.Admin;

namespace LittleEcommerce.Source.Application.Services;

public class RoleService(
    IUnitOfWork unitOfWork,
    IRoleRepository roles,
    IPermissionRepository permissions,
    IRolePermissionService rolePermissions,
    ILogger<RoleService> logger) : IRoleService
{
    private const string PermissionsField = "permissions";

    public async Task<IReadOnlyList<RoleResult>> ListAsync(CancellationToken ct = default)
    {
        var all = await roles.ListWithPermissionsAsync(ct);
        var counts = await roles.CountUsersByRoleAsync(ct);
        var catalog = (await permissions.ListAsync(ct)).Select(p => p.Code).ToList();

        return all
            .Select(role => ToResult(role, counts.GetValueOrDefault(role.Id), role.Name == Roles.Admin ? catalog : null))
            .ToList();
    }

    public async Task<IReadOnlyList<PermissionResult>> ListPermissionsAsync(CancellationToken ct = default) =>
        (await permissions.ListAsync(ct))
            .Select(p => new PermissionResult(p.Code, p.Group, p.Description))
            .ToList();

    public async Task<RoleResult> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await roles.ExistsByNameAsync(name, ct))
            throw new ConflictException($"Já existe um papel chamado {name}", ErrorCodes.RoleAlreadyExists);

        var granted = await ResolvePermissionsAsync(request.Permissions, ct);

        var role = new Role { Name = name, Description = request.Description?.Trim() };

        foreach (var permission in granted)
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission, PermissionId = permission.Id });

        roles.Add(role);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Papel {Role} criado com {Count} permissões", role.Name, granted.Count);

        return ToResult(role, 0);
    }

    public async Task<RoleResult> UpdatePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await roles.GetWithPermissionsAsync(roleId, ct)
                   ?? throw new NotFoundException("Papel não encontrado");

        if (role.Name == Roles.Admin)
            throw new DomainException("O papel Admin sempre tem todas as permissões", ErrorCodes.AdminRoleLocked);

        var granted = await ResolvePermissionsAsync(request.Permissions, ct);
        var wanted = granted.Select(p => p.Id).ToHashSet();

        foreach (var current in role.RolePermissions.Where(rp => !wanted.Contains(rp.PermissionId)).ToList())
            role.RolePermissions.Remove(current);

        var existing = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        foreach (var permission in granted.Where(p => !existing.Contains(p.Id)))
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Permission = permission });

        await unitOfWork.SaveChangesAsync(ct);
        await rolePermissions.InvalidateAsync(role.Name);

        logger.LogInformation("Permissões do papel {Role} atualizadas: {Permissions}", role.Name, string.Join(',', granted.Select(p => p.Code)));

        var counts = await roles.CountUsersByRoleAsync(ct);
        return ToResult(role, counts.GetValueOrDefault(role.Id));
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await roles.GetWithPermissionsAsync(roleId, ct)
                   ?? throw new NotFoundException("Papel não encontrado");

        if (role.IsSystem)
            throw new DomainException($"O papel {role.Name} é do sistema e não pode ser removido", ErrorCodes.SystemRoleLocked);

        if (await roles.HasUsersAsync(role.Id, ct))
            throw new ConflictException($"O papel {role.Name} ainda tem usuários", ErrorCodes.RoleInUse);

        roles.Remove(role);
        await unitOfWork.SaveChangesAsync(ct);
        await rolePermissions.InvalidateAsync(role.Name);

        logger.LogInformation("Papel {Role} removido", role.Name);
    }

    private async Task<IReadOnlyList<Permission>> ResolvePermissionsAsync(IReadOnlyList<string> codes, CancellationToken ct)
    {
        var wanted = codes.Select(c => c.Trim()).Where(c => c.Length > 0).Distinct().ToList();
        var found = await permissions.GetByCodesAsync(wanted, ct);
        var unknown = wanted.Except(found.Select(p => p.Code)).ToList();

        if (unknown.Count > 0)
            throw new ValidationException(PermissionsField, $"Permissões inexistentes: {string.Join(", ", unknown)}");

        return found;
    }

    private static RoleResult ToResult(Role role, int users, IReadOnlyList<string>? overridePermissions = null) => new(
        role.Id,
        role.Name,
        role.Description,
        role.IsSystem,
        overridePermissions ?? role.RolePermissions.Select(rp => rp.Permission.Code).Order().ToList(),
        users);
}
