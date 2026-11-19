using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Results.Meta;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Store;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Sistema")]
public class SystemController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("version")]
    [EndpointSummary("Versão da API")]
    [EndpointDescription("Nome, versão e ambiente em que a API está rodando.")]
    [ProducesResponseType<APISuccessResponse<SystemVersionResult>>(StatusCodes.Status200OK)]
    public IActionResult Version() =>
        APIResponse.Success(new SystemVersionResult(AppInfo.Name, AppInfo.Version, environment.EnvironmentName));
}
