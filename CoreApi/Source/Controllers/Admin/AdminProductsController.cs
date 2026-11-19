using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Authorization;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Requests.Catalog;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using LittleEcommerce.Source.Shared.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Admin;

[Route(RoutePrefix + "/products")]
[Tags("Admin · Produtos")]
public class AdminProductsController(IProductAdminService service) : AdminControllerBase
{
    [HttpGet]
    [RequiresPermission(Permissions.CatalogRead)]
    [EndpointSummary("Listar produtos (inclusive inativos)")]
    [ProducesResponseType<PaginatedResponse<AdminProductResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] AdminProductFilter filter, CancellationToken ct)
    {
        var (items, total) = await service.ListAsync(filter, ct);
        return APIResponse.Paginated(items, total, filter.Limit, filter.Offset);
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(Permissions.CatalogRead)]
    [EndpointSummary("Detalhar produto")]
    [ProducesResponseType<APISuccessResponse<AdminProductResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        APIResponse.Success(await service.GetAsync(id, ct));

    [HttpPost]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Criar produto")]
    [EndpointDescription("Sem `slug`, ele é gerado a partir do nome (com sufixo se já existir). O SKU é gravado em maiúsculas.")]
    [ProducesResponseType<APISuccessResponse<AdminProductResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct) =>
        APIResponse.Created(await service.CreateAsync(request, ct), "Produto criado");

    [HttpPut("{id:guid}")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Atualizar produto")]
    [EndpointDescription("Envie a `version` que você leu: se outra pessoa alterou o produto nesse meio-tempo, a resposta é 409 `CONCURRENCY_CONFLICT`.")]
    [ProducesResponseType<APISuccessResponse<AdminProductResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        APIResponse.Success(await service.UpdateAsync(id, request, ct), "Produto atualizado");

    [HttpPatch("{id:guid}/status")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Ativar ou desativar produto")]
    [ProducesResponseType<APISuccessResponse<AdminProductResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] UpdateProductStatusRequest request, CancellationToken ct) =>
        APIResponse.Success(await service.SetStatusAsync(id, request.IsActive, ct), request.IsActive ? "Produto ativado" : "Produto desativado");

    [HttpPatch("{id:guid}/stock")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Definir estoque")]
    [ProducesResponseType<APISuccessResponse<AdminProductResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetStock(Guid id, [FromBody] UpdateStockRequest request, CancellationToken ct) =>
        APIResponse.Success(await service.SetStockAsync(id, request.Quantity, ct), "Estoque atualizado");

    [HttpDelete("{id:guid}")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Remover produto")]
    [EndpointDescription("Remoção lógica (some do catálogo, fica no histórico) e apaga os vínculos do produto.")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return APIResponse.Success(new { }, "Produto removido");
    }

    [HttpGet("{id:guid}/links")]
    [RequiresPermission(Permissions.CatalogRead)]
    [EndpointSummary("Listar vínculos do produto")]
    [ProducesResponseType<APISuccessResponse<IReadOnlyList<ProductLinkResult>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Links(Guid id, CancellationToken ct) =>
        APIResponse.Success(await service.ListLinksAsync(id, ct));

    [HttpPost("{id:guid}/links")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Vincular produto")]
    [EndpointDescription(
        "`Related` e `Substitute` valem nos dois sentidos (o vínculo inverso é criado junto). " +
        "`Accessory` e `Kit` valem só daqui para o vinculado; `quantity` só é usada em `Kit`.")]
    [ProducesResponseType<APISuccessResponse<ProductLinkResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddLink(Guid id, [FromBody] CreateProductLinkRequest request, CancellationToken ct) =>
        APIResponse.Created(await service.AddLinkAsync(id, request, ct), "Vínculo criado");

    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Remover vínculo")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLink(Guid id, Guid linkId, CancellationToken ct)
    {
        await service.RemoveLinkAsync(id, linkId, ct);
        return APIResponse.Success(new { }, "Vínculo removido");
    }
}
