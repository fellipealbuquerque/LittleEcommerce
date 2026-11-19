using LittleEcommerce.Source.Shared.DTOs.Requests.Users;
using LittleEcommerce.Source.Shared.DTOs.Results.Users;

namespace LittleEcommerce.Source.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserResult> GetAsync(Guid userId, CancellationToken ct = default);

    Task<UserResult> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}
