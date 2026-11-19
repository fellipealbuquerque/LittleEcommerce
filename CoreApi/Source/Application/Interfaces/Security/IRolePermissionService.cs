namespace LittleEcommerce.Source.Application.Interfaces.Security;

public interface IRolePermissionService
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(string roleName, CancellationToken ct = default);

    Task InvalidateAsync(string roleName);
}
