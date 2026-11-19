using LittleEcommerce.Source.Application.Caching;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces.Caching;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Mappers;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Application.Services;

public class CatalogService(
    IProductRepository products,
    ICategoryRepository categories,
    IProductLinkRepository links,
    ICatalogCache cache) : ICatalogService
{
    public Task<CatalogPage<ProductSummaryResult>> ListProductsAsync(ProductFilter filter, CancellationToken ct = default) =>
        cache.GetOrCreateAsync(CatalogCacheKeys.ProductList(filter), async () =>
        {
            Guid? categoryId = null;

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                var category = await categories.FindPublicAsync(filter.Category, ct);

                if (category is null)
                    return new CatalogPage<ProductSummaryResult>([], 0);

                categoryId = category.Id;
            }

            var (items, total) = await products.SearchPublicAsync(filter, categoryId, ct);
            return new CatalogPage<ProductSummaryResult>(items.Select(p => p.ToSummary()).ToList(), total);
        });

    public async Task<ProductResult> GetProductAsync(string idOrSlug, CancellationToken ct = default)
    {
        var result = await cache.GetOrCreateAsync(CatalogCacheKeys.Product(idOrSlug), async () =>
            (await products.FindPublicAsync(idOrSlug, ct))?.ToResult());

        return result ?? throw new NotFoundException("Produto não encontrado");
    }

    public async Task<IReadOnlyList<ProductLinkResult>> GetLinksAsync(string idOrSlug, ProductLinkType? type, CancellationToken ct = default)
    {
        var result = await cache.GetOrCreateAsync(CatalogCacheKeys.Links(idOrSlug, type), async () =>
        {
            var product = await products.FindPublicAsync(idOrSlug, ct);

            if (product is null)
                return null;

            return (await links.ListAsync(product.Id, type, onlyPublic: true, ct)).Select(l => l.ToResult()).ToList();
        });

        return result ?? throw new NotFoundException("Produto não encontrado");
    }

    public Task<IReadOnlyList<CategoryResult>> ListCategoriesAsync(CancellationToken ct = default) =>
        cache.GetOrCreateAsync<IReadOnlyList<CategoryResult>>(CatalogCacheKeys.Categories, async () =>
            (await categories.ListPublicAsync(ct)).Select(c => c.Category.ToResult(c.Products)).ToList());
}
