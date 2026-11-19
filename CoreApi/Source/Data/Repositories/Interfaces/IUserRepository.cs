using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);

    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct);

    Task<int?> GetActiveSessionVersionAsync(Guid id, CancellationToken ct);

    Task<(IReadOnlyList<User> Items, int Total)> ListAsync(AdminUserFilter filter, CancellationToken ct);

    Task<int> CountActiveInRoleAsync(Guid roleId, CancellationToken ct);

    void Add(User user);
}
