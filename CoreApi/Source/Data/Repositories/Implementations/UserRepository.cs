using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.Filters;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<int?> GetActiveSessionVersionAsync(Guid id, CancellationToken ct) =>
        context.Users
            .AsNoTracking()
            .Where(u => u.Id == id && u.Status == UserStatus.Active)
            .Select(u => (int?)u.SessionVersion)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<User> Items, int Total)> ListAsync(AdminUserFilter filter, CancellationToken ct)
    {
        var query = context.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
            query = query.Where(u => u.Role.Name == filter.Role);

        if (filter.Status is { } status)
            query = query.Where(u => u.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<int> CountActiveInRoleAsync(Guid roleId, CancellationToken ct) =>
        context.Users.CountAsync(u => u.RoleId == roleId && u.Status == UserStatus.Active, ct);

    public void Add(User user) => context.Users.Add(user);
}
