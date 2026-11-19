using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Mappers;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Users;
using LittleEcommerce.Source.Shared.DTOs.Results.Users;
using LittleEcommerce.Source.Shared.Extensions;

namespace LittleEcommerce.Source.Application.Services;

public class UserService(
    IUnitOfWork unitOfWork,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher hasher,
    ITokenRevocationStore revocation,
    TimeProvider clock,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserResult> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("Usuário não encontrado");

        return user.ToResult();
    }

    public async Task<UserResult> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("Usuário não encontrado");

        user.Name = request.Name.Trim();
        await unitOfWork.SaveChangesAsync(ct);

        return user.ToResult();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var version = await unitOfWork.ExecuteAsync(async () =>
        {
            var user = await users.GetByIdAsync(userId, ct)
                       ?? throw new NotFoundException("Usuário não encontrado");

            if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new ValidationException(nameof(request.CurrentPassword).ToCamelCase(), "Senha atual incorreta");

            if (hasher.Verify(request.NewPassword, user.PasswordHash))
                throw new ValidationException(nameof(request.NewPassword).ToCamelCase(), "A nova senha precisa ser diferente da atual");

            user.PasswordHash = hasher.Hash(request.NewPassword);
            user.InvalidateSessions();
            await refreshTokens.RevokeAllForUserAsync(userId, clock.GetUtcNow().UtcDateTime, RevocationReasons.PasswordChanged, ct);

            return user.SessionVersion;
        }, ct);

        await revocation.PublishSessionVersionAsync(userId, version);

        logger.LogInformation("Senha alterada pelo usuário {UserId}; sessões encerradas", userId);
    }
}
