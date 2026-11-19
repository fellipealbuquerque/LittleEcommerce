using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);

    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct);

    Task<int?> GetActiveSessionVersionAsync(Guid id, CancellationToken ct);

    void Add(User user);
}
