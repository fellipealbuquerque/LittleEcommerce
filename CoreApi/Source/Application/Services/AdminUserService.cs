using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Security;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Mappers;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.Source.Shared.DTOs.Results.Users;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Application.Services;

public class AdminUserService(
    IUnitOfWork unitOfWork,
    IUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    ITokenRevocationStore revocation,
    TimeProvider clock,
    ILogger<AdminUserService> logger) : IAdminUserService
{
    private const string RoleField = "role";

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<(IReadOnlyList<UserResult> Items, int Total)> ListAsync(AdminUserFilter filter, CancellationToken ct = default)
    {
        var (items, total) = await users.ListAsync(filter, ct);
        return (items.Select(u => u.ToResult()).ToList(), total);
    }

    public async Task<UserResult> GetAsync(Guid userId, CancellationToken ct = default) =>
        (await FindAsync(userId, ct)).ToResult();

    public async Task<UserResult> ChangeRoleAsync(Guid actorId, Guid userId, ChangeUserRoleRequest request, CancellationToken ct = default)
    {
        EnsureNotSelf(actorId, userId, "trocar o próprio papel");

        var user = await unitOfWork.ExecuteAsync(async () =>
        {
            var target = await FindAsync(userId, ct);
            var role = await roles.GetByNameAsync(request.Role.Trim(), ct)
                       ?? throw new ValidationException(RoleField, "Papel não encontrado");

            if (target.RoleId == role.Id)
                return target;

            await EnsureNotLastAdminAsync(target, ct);

            target.RoleId = role.Id;
            target.Role = role;
            await EndSessionsAsync(target, RevocationReasons.RoleChanged, ct);

            return target;
        }, ct);

        await revocation.PublishSessionVersionAsync(user.Id, user.SessionVersion);
        logger.LogInformation("Usuário {UserId} passou para o papel {Role} por {ActorId}", user.Id, user.Role.Name, actorId);

        return user.ToResult();
    }

    public async Task<UserResult> BlockAsync(Guid actorId, Guid userId, CancellationToken ct = default)
    {
        EnsureNotSelf(actorId, userId, "bloquear a própria conta");

        var user = await unitOfWork.ExecuteAsync(async () =>
        {
            var target = await FindAsync(userId, ct);

            if (target.Status == UserStatus.Blocked)
                return target;

            await EnsureNotLastAdminAsync(target, ct);

            target.Status = UserStatus.Blocked;
            await EndSessionsAsync(target, RevocationReasons.Blocked, ct);

            return target;
        }, ct);

        await revocation.PublishSessionVersionAsync(user.Id, user.SessionVersion);
        logger.LogInformation("Usuário {UserId} bloqueado por {ActorId}", user.Id, actorId);

        return user.ToResult();
    }

    public async Task<UserResult> UnblockAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await FindAsync(userId, ct);

        if (user.Status == UserStatus.Active)
            return user.ToResult();

        user.Status = UserStatus.Active;
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Usuário {UserId} desbloqueado", user.Id);

        return user.ToResult();
    }

    public async Task RevokeSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await unitOfWork.ExecuteAsync(async () =>
        {
            var target = await FindAsync(userId, ct);
            await EndSessionsAsync(target, RevocationReasons.AdminRevoked, ct);
            return target;
        }, ct);

        await revocation.PublishSessionVersionAsync(user.Id, user.SessionVersion);
        logger.LogInformation("Sessões do usuário {UserId} encerradas pela administração", user.Id);
    }

    private async Task<User> FindAsync(Guid userId, CancellationToken ct) =>
        await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Usuário não encontrado");

    private async Task EndSessionsAsync(User user, string reason, CancellationToken ct)
    {
        user.InvalidateSessions();
        await refreshTokens.RevokeAllForUserAsync(user.Id, Now, reason, ct);
    }

    private async Task EnsureNotLastAdminAsync(User target, CancellationToken ct)
    {
        if (target.Role.Name != Roles.Admin || target.Status != UserStatus.Active)
            return;

        if (await users.CountActiveInRoleAsync(target.RoleId, ct) <= 1)
            throw new DomainException("Não é possível remover o último administrador ativo", ErrorCodes.LastAdmin);
    }

    private static void EnsureNotSelf(Guid actorId, Guid userId, string action)
    {
        if (actorId == userId)
            throw new DomainException($"Você não pode {action}", ErrorCodes.CannotChangeOwnAccount);
    }
}
