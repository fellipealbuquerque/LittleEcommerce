using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.Source.Shared.DTOs.Results.Users;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Application.Interfaces.Services;

public interface IAdminUserService
{
    Task<(IReadOnlyList<UserResult> Items, int Total)> ListAsync(AdminUserFilter filter, CancellationToken ct = default);

    Task<UserResult> GetAsync(Guid userId, CancellationToken ct = default);

    Task<UserResult> ChangeRoleAsync(Guid actorId, Guid userId, ChangeUserRoleRequest request, CancellationToken ct = default);

    Task<UserResult> BlockAsync(Guid actorId, Guid userId, CancellationToken ct = default);

    Task<UserResult> UnblockAsync(Guid userId, CancellationToken ct = default);

    Task RevokeSessionsAsync(Guid userId, CancellationToken ct = default);
}
