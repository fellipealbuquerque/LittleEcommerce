using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Auditing;
using LittleEcommerce.Source.Infrastructure.Authorization;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Results.Audit;
using LittleEcommerce.Source.Shared.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Admin;

[Route(RoutePrefix + "/audit-logs")]
[Tags("Admin · Auditoria")]
public class AdminAuditLogsController(IAuditLogReader reader) : AdminControllerBase
{
    [HttpGet]
    [RequiresPermission(Permissions.AuditRead)]
    [EndpointSummary("Consultar auditoria")]
    [EndpointDescription(
        "Registros mais recentes primeiro. `kind`: `entity-change` (alteração de dados com antes e depois), " +
        "`admin-action` (toda chamada a rota admin) ou `access-denied` (403). Filtros: `action`, `actorId`, " +
        "`entityType`, `entityId`, `correlationId`, `from`, `to`.")]
    [ProducesResponseType<PaginatedResponse<AuditLogResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] AuditLogFilter filter, CancellationToken ct)
    {
        var (items, total) = await reader.ListAsync(filter, ct);
        return APIResponse.Paginated(items, (int)Math.Min(total, int.MaxValue), filter.Limit, filter.Offset);
    }
}
