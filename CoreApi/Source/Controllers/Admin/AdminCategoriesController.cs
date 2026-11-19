using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Authorization;
using LittleEcommerce.Source.Shared.DTOs;
using LittleEcommerce.Source.Shared.DTOs.Requests.Catalog;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace LittleEcommerce.Source.Controllers.Admin;

[Route(RoutePrefix + "/categories")]
[Tags("Admin · Categorias")]
public class AdminCategoriesController(ICategoryAdminService service) : AdminControllerBase
{
    [HttpGet]
    [RequiresPermission(Permissions.CatalogRead)]
    [EndpointSummary("Listar categorias (inclusive inativas)")]
    [ProducesResponseType<APISuccessResponse<IReadOnlyList<CategoryResult>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        APIResponse.Success(await service.ListAsync(ct));

    [HttpPost]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Criar categoria")]
    [ProducesResponseType<APISuccessResponse<CategoryResult>>(StatusCodes.Status201Created)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct) =>
        APIResponse.Created(await service.CreateAsync(request, ct), "Categoria criada");

    [HttpPut("{id:guid}")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Atualizar categoria")]
    [EndpointDescription("Desativar a categoria esconde os produtos dela do catálogo público.")]
    [ProducesResponseType<APISuccessResponse<CategoryResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct) =>
        APIResponse.Success(await service.UpdateAsync(id, request, ct), "Categoria atualizada");

    [HttpDelete("{id:guid}")]
    [RequiresPermission(Permissions.CatalogManage)]
    [EndpointSummary("Remover categoria")]
    [EndpointDescription("Só categorias sem produtos.")]
    [ProducesResponseType<APISuccessResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<APIErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return APIResponse.Success(new { }, "Categoria removida");
    }
}
