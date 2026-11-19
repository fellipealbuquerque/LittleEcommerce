using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Authorization;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Requests.Admin;
using LittleEcommerce.Source.Shared.DTOs.Results.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Admin;

[Route(RoutePrefix + "/roles")]
[Tags("Admin · Papéis e permissões")]
public class AdminRolesController(IRoleService service) : AdminControllerBase
{
    [HttpGet]
    [RequiresPermission(Permissions.RolesRead)]
    [EndpointSummary("Listar papéis")]
    [EndpointDescription("Papéis com suas permissões e quantidade de usuários. O Admin sempre tem todas.")]
    [ProducesResponseType<APISuccessResponse<IReadOnlyList<RoleResult>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        APIResponse.Success(await service.ListAsync(ct));

    [HttpGet("~/" + RoutePrefix + "/permissions")]
    [RequiresPermission(Permissions.RolesRead)]
    [EndpointSummary("Catálogo de permissões")]
    [ProducesResponseType<APISuccessResponse<IReadOnlyList<PermissionResult>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPermissions(CancellationToken ct) =>
        APIResponse.Success(await service.ListPermissionsAsync(ct));

    [HttpPost]
    [RequiresPermission(Permissions.RolesManage)]
    [EndpointSummary("Criar papel")]
    [ProducesResponseType<APISuccessResponse<RoleResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct) =>
        APIResponse.Created(await service.CreateAsync(request, ct), "Papel criado");

    [HttpPut("{id:guid}/permissions")]
    [RequiresPermission(Permissions.RolesManage)]
    [EndpointSummary("Definir permissões do papel")]
    [EndpointDescription("Substitui o conjunto de permissões. Vale na próxima requisição de quem tem o papel.")]
    [ProducesResponseType<APISuccessResponse<RoleResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct) =>
        APIResponse.Success(await service.UpdatePermissionsAsync(id, request, ct), "Permissões atualizadas");

    [HttpDelete("{id:guid}")]
    [RequiresPermission(Permissions.RolesManage)]
    [EndpointSummary("Remover papel")]
    [EndpointDescription("Papéis do sistema e papéis com usuários não podem ser removidos.")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return APIResponse.Success(new { }, "Papel removido");
    }
}
