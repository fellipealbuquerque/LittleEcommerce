using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct);
}
