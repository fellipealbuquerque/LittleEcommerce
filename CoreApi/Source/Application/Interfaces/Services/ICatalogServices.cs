using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Shared.DTOs.Requests.Catalog;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Application.Interfaces.Services;

public interface ICatalogService
{
    Task<CatalogPage<ProductSummaryResult>> ListProductsAsync(ProductFilter filter, CancellationToken ct = default);

    Task<ProductResult> GetProductAsync(string idOrSlug, CancellationToken ct = default);

    Task<IReadOnlyList<ProductLinkResult>> GetLinksAsync(string idOrSlug, ProductLinkType? type, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryResult>> ListCategoriesAsync(CancellationToken ct = default);
}

public interface IProductAdminService
{
    Task<(IReadOnlyList<AdminProductResult> Items, int Total)> ListAsync(AdminProductFilter filter, CancellationToken ct = default);

    Task<AdminProductResult> GetAsync(Guid id, CancellationToken ct = default);

    Task<AdminProductResult> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    Task<AdminProductResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);

    Task<AdminProductResult> SetStatusAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task<AdminProductResult> SetStockAsync(Guid id, int quantity, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ProductLinkResult>> ListLinksAsync(Guid id, CancellationToken ct = default);

    Task<ProductLinkResult> AddLinkAsync(Guid id, CreateProductLinkRequest request, CancellationToken ct = default);

    Task RemoveLinkAsync(Guid id, Guid linkId, CancellationToken ct = default);
}

public interface ICategoryAdminService
{
    Task<IReadOnlyList<CategoryResult>> ListAsync(CancellationToken ct = default);

    Task<CategoryResult> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);

    Task<CategoryResult> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
