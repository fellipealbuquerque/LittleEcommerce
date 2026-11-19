using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Authorization;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Results.Admin;
using LittleEcommerce.Source.Shared.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Admin;

[Route(RoutePrefix + "/outbox")]
[Tags("Admin · Outbox")]
public class AdminOutboxController(IOutboxAdminService service) : AdminControllerBase
{
    [HttpGet]
    [RequiresPermission(Permissions.OutboxRead)]
    [EndpointSummary("Listar mensagens do outbox")]
    [EndpointDescription("Filtra por `status` (Pending, Processing, Sent, DeadLetter), `type` e `aggregateId` (ex.: id do usuário).")]
    [ProducesResponseType<PaginatedResponse<OutboxMessageResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] OutboxFilter filter, CancellationToken ct)
    {
        var (items, total) = await service.ListAsync(filter, ct);
        return APIResponse.Paginated(items, total, filter.Limit, filter.Offset);
    }

    [HttpGet("summary")]
    [RequiresPermission(Permissions.OutboxRead)]
    [EndpointSummary("Resumo do outbox por status")]
    [ProducesResponseType<APISuccessResponse<OutboxSummaryResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken ct) =>
        APIResponse.Success(await service.SummaryAsync(ct));

    [HttpGet("{id:guid}")]
    [RequiresPermission(Permissions.OutboxRead)]
    [EndpointSummary("Detalhar mensagem")]
    [ProducesResponseType<APISuccessResponse<OutboxMessageResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        APIResponse.Success(await service.GetAsync(id, ct));

    [HttpPost("{id:guid}/retry")]
    [RequiresPermission(Permissions.OutboxManage)]
    [EndpointSummary("Reenviar mensagem")]
    [EndpointDescription("Volta a mensagem para a fila com as tentativas zeradas, inclusive se já foi enviada. O dispatcher é acordado na hora.")]
    [ProducesResponseType<APISuccessResponse<OutboxMessageResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct) =>
        APIResponse.Success(await service.RetryAsync(id, ct), "Mensagem de volta na fila");

    [HttpPost("retry-dead-letters")]
    [RequiresPermission(Permissions.OutboxManage)]
    [EndpointSummary("Reenviar todas as mensagens em dead-letter")]
    [EndpointDescription("Opcionalmente só de um `type`.")]
    [ProducesResponseType<APISuccessResponse<OutboxRequeueResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryDeadLetters([FromQuery] string? type, CancellationToken ct) =>
        APIResponse.Success(await service.RetryDeadLettersAsync(type, ct), "Mensagens de volta na fila");
}
