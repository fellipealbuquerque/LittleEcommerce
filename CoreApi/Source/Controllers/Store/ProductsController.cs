using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using LittleEcommerce.Source.Shared.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Store;

[ApiController]
[Route("api/v1/[controller]")]
[Tags("Catálogo")]
public class ProductsController(ICatalogService service) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Listar produtos")]
    [EndpointDescription(
        "Só produtos ativos de categorias ativas. Filtros: `search` (nome, SKU ou descrição), `category` (id ou slug), " +
        "`minPrice`, `maxPrice`, `inStock`. Ordenação `sort`: `Newest`, `NameAsc`, `PriceAsc`, `PriceDesc`. Paginado por `limit` e `offset`.")]
    [ProducesResponseType<PaginatedResponse<ProductSummaryResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] ProductFilter filter, CancellationToken ct)
    {
        var page = await service.ListProductsAsync(filter, ct);
        return APIResponse.Paginated(page.Items, page.Total, filter.Limit, filter.Offset);
    }

    [HttpGet("{idOrSlug}")]
    [EndpointSummary("Detalhar produto")]
    [EndpointDescription("Aceita o id ou o slug.")]
    [ProducesResponseType<APISuccessResponse<ProductResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string idOrSlug, CancellationToken ct) =>
        APIResponse.Success(await service.GetProductAsync(idOrSlug, ct));

    [HttpGet("{idOrSlug}/links")]
    [EndpointSummary("Produtos vinculados")]
    [EndpointDescription("Relacionados, acessórios, componentes do kit (com quantidade) e substitutos. Filtre com `type`.")]
    [ProducesResponseType<APISuccessResponse<IReadOnlyList<ProductLinkResult>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Links(string idOrSlug, [FromQuery] ProductLinkType? type, CancellationToken ct) =>
        APIResponse.Success(await service.GetLinksAsync(idOrSlug, type, ct));
}
