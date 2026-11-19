using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
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

    public void Add(User user) => context.Users.Add(user);
}
