using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Authorization;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.Source.Shared.DTOs.Results.Users;
using LittleEcommerce.Source.Shared.Extensions;
using LittleEcommerce.Source.Shared.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Admin;

[Route(RoutePrefix + "/users")]
[Tags("Admin · Usuários")]
public class AdminUsersController(IAdminUserService service) : AdminControllerBase
{
    [HttpGet]
    [RequiresPermission(Permissions.UsersRead)]
    [EndpointSummary("Listar usuários")]
    [EndpointDescription("Busca por nome ou e-mail (`search`), filtra por `role` e `status`, paginado por `limit` e `offset`.")]
    [ProducesResponseType<PaginatedResponse<UserResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] AdminUserFilter filter, CancellationToken ct)
    {
        var (items, total) = await service.ListAsync(filter, ct);
        return APIResponse.Paginated(items, total, filter.Limit, filter.Offset);
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(Permissions.UsersRead)]
    [EndpointSummary("Detalhar usuário")]
    [ProducesResponseType<APISuccessResponse<UserResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        APIResponse.Success(await service.GetAsync(id, ct));

    [HttpPatch("{id:guid}/role")]
    [RequiresPermission(Permissions.UsersManage)]
    [EndpointSummary("Trocar papel do usuário")]
    [EndpointDescription("Troca o papel e encerra as sessões do usuário, que precisa entrar de novo para receber o papel novo no token.")]
    [ProducesResponseType<APISuccessResponse<UserResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeUserRoleRequest request, CancellationToken ct) =>
        APIResponse.Success(await service.ChangeRoleAsync(this.UserId(), id, request, ct), "Papel alterado");

    [HttpPatch("{id:guid}/block")]
    [RequiresPermission(Permissions.UsersManage)]
    [EndpointSummary("Bloquear usuário")]
    [EndpointDescription("Bloqueia o login e derruba todas as sessões do usuário.")]
    [ProducesResponseType<APISuccessResponse<UserResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Block(Guid id, CancellationToken ct) =>
        APIResponse.Success(await service.BlockAsync(this.UserId(), id, ct), "Usuário bloqueado");

    [HttpPatch("{id:guid}/unblock")]
    [RequiresPermission(Permissions.UsersManage)]
    [EndpointSummary("Desbloquear usuário")]
    [ProducesResponseType<APISuccessResponse<UserResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken ct) =>
        APIResponse.Success(await service.UnblockAsync(id, ct), "Usuário desbloqueado");

    [HttpPost("{id:guid}/revoke-sessions")]
    [RequiresPermission(Permissions.UsersManage)]
    [EndpointSummary("Encerrar sessões do usuário")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeSessions(Guid id, CancellationToken ct)
    {
        await service.RevokeSessionsAsync(id, ct);
        return APIResponse.Success(new { }, "Sessões encerradas");
    }
}
