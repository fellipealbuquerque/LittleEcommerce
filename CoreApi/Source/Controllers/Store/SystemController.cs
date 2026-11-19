using System.Reflection;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Results.Meta;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Store;

[ApiController]
[Route("api/v1/[controller]")]
public class SystemController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("version")]
    public IActionResult Version()
    {
        var assembly = typeof(Program).Assembly;

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.0.0";

        var result = new SystemVersionResult(
            assembly.GetName().Name ?? "LittleEcommerce",
            informational.Split('+')[0],
            environment.EnvironmentName);

        return APIResponse.Success(result);
    }
}
