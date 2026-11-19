using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.Source.Shared.DTOs.Results.Admin;

namespace LittleEcommerce.Source.Application.Interfaces.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResult>> ListAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PermissionResult>> ListPermissionsAsync(CancellationToken ct = default);

    Task<RoleResult> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);

    Task<RoleResult> UpdatePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid roleId, CancellationToken ct = default);
}
