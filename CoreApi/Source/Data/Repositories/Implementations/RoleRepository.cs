using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken ct) =>
        context.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken ct) =>
        context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Role>> ListWithPermissionsAsync(CancellationToken ct) =>
        await context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, int>> CountUsersByRoleAsync(CancellationToken ct) =>
        await context.Users
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.RoleId, g => g.Count, ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct) =>
        context.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower(), ct);

    public Task<bool> HasUsersAsync(Guid roleId, CancellationToken ct) =>
        context.Users.IgnoreQueryFilters().AnyAsync(u => u.RoleId == roleId, ct);

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(string roleName, CancellationToken ct) =>
        await context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Role.Name == roleName)
            .Select(rp => rp.Permission.Code)
            .ToListAsync(ct);

    public void Add(Role role) => context.Roles.Add(role);

    public void Remove(Role role) => context.Roles.Remove(role);
}
