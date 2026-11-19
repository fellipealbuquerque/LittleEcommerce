using LittleEcommerce.Source.Infrastructure.Auditing;
using LittleEcommerce.Source.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Admin;

[ApiController]
[Authorize]
[ServiceFilter<AdminAuditFilter>]
[ProducesResponseType<APIErrorResponse>(StatusCodes.Status403Forbidden)]
public abstract class AdminControllerBase : ControllerBase
{
    public const string RoutePrefix = "api/v1/admin";
}
