using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class PermissionRepository(ApplicationDbContext context) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct) =>
        await context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Code)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var wanted = codes.Distinct().ToList();
        return await context.Permissions.Where(p => wanted.Contains(p.Code)).ToListAsync(ct);
    }
}
