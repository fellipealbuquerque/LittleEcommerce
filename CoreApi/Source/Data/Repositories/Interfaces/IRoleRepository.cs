using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct);

    Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Role>> ListWithPermissionsAsync(CancellationToken ct);

    Task<IReadOnlyDictionary<Guid, int>> CountUsersByRoleAsync(CancellationToken ct);

    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);

    Task<bool> HasUsersAsync(Guid roleId, CancellationToken ct);

    Task<IReadOnlyList<string>> GetPermissionCodesAsync(string roleName, CancellationToken ct);

    void Add(Role role);

    void Remove(Role role);
}
