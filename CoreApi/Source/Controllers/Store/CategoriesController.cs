using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Store;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Catálogo")]
public class CategoriesController(ICatalogService service) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Listar categorias")]
    [EndpointDescription("Categorias ativas com a quantidade de produtos ativos.")]
    [ProducesResponseType<APISuccessResponse<IReadOnlyList<CategoryResult>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        APIResponse.Success(await service.ListCategoriesAsync(ct));
}
