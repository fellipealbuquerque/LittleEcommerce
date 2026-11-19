using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct);

    Task<IReadOnlyList<Permission>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct);
}
