using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken ct) =>
        context.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);
}
