using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, CancellationToken ct);

    Task<Product?> FindPublicAsync(string idOrSlug, CancellationToken ct);

    Task<bool> SkuExistsAsync(string sku, Guid? exceptId, CancellationToken ct);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct);

    Task<(IReadOnlyList<Product> Items, int Total)> SearchPublicAsync(ProductFilter filter, Guid? categoryId, CancellationToken ct);

    Task<(IReadOnlyList<Product> Items, int Total)> ListAdminAsync(AdminProductFilter filter, CancellationToken ct);

    void Add(Product product);

    void Remove(Product product);

    void ExpectVersion(Product product, uint version);
}

public interface ICategoryRepository
{
    Task<Category?> GetAsync(Guid id, CancellationToken ct);

    Task<Category?> FindPublicAsync(string idOrSlug, CancellationToken ct);

    Task<IReadOnlyList<(Category Category, int Products)>> ListPublicAsync(CancellationToken ct);

    Task<IReadOnlyList<(Category Category, int Products)>> ListAdminAsync(CancellationToken ct);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct);

    Task<bool> HasProductsAsync(Guid id, CancellationToken ct);

    Task<int> CountProductsAsync(Guid id, CancellationToken ct);

    void Add(Category category);

    void Remove(Category category);
}

public interface IProductLinkRepository
{
    Task<IReadOnlyList<ProductLink>> ListAsync(Guid productId, ProductLinkType? type, bool onlyPublic, CancellationToken ct);

    Task<ProductLink?> GetAsync(Guid productId, Guid linkId, CancellationToken ct);

    Task<ProductLink?> FindAsync(Guid productId, Guid linkedProductId, ProductLinkType type, CancellationToken ct);

    Task<IReadOnlyList<ProductLink>> ListTouchingAsync(Guid productId, CancellationToken ct);

    void Add(ProductLink link);

    void Remove(ProductLink link);
}
