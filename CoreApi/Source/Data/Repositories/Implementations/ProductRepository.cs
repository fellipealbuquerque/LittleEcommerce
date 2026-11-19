using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.Filters;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    public Task<Product?> GetAsync(Guid id, CancellationToken ct) =>
        context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> FindPublicAsync(string idOrSlug, CancellationToken ct)
    {
        var query = PublicProducts();

        return Guid.TryParse(idOrSlug, out var id)
            ? query.FirstOrDefaultAsync(p => p.Id == id, ct)
            : query.FirstOrDefaultAsync(p => p.Slug == idOrSlug.ToLowerInvariant(), ct);
    }

    public Task<bool> SkuExistsAsync(string sku, Guid? exceptId, CancellationToken ct) =>
        context.Products.AnyAsync(p => p.Sku == sku && p.Id != exceptId, ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct) =>
        context.Products.AnyAsync(p => p.Slug == slug && p.Id != exceptId, ct);

    public async Task<(IReadOnlyList<Product> Items, int Total)> SearchPublicAsync(ProductFilter filter, Guid? categoryId, CancellationToken ct)
    {
        var query = PublicProducts();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(p.Sku, pattern, LikePattern.EscapeCharacter)
                || (p.Description != null && EF.Functions.ILike(p.Description, pattern, LikePattern.EscapeCharacter)));
        }

        if (categoryId is { } category)
            query = query.Where(p => p.CategoryId == category);

        if (filter.MinPrice is { } min)
            query = query.Where(p => p.Price >= min);

        if (filter.MaxPrice is { } max)
            query = query.Where(p => p.Price <= max);

        if (filter.InStock is { } inStock)
            query = inStock ? query.Where(p => p.Stock > 0) : query.Where(p => p.Stock == 0);

        var total = await query.CountAsync(ct);

        var sorted = filter.Sort switch
        {
            ProductSort.NameAsc => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            ProductSort.PriceAsc => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            ProductSort.PriceDesc => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        var items = await sorted.Skip(filter.Offset).Take(filter.Limit).ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Product> Items, int Total)> ListAdminAsync(AdminProductFilter filter, CancellationToken ct)
    {
        var query = context.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(p.Sku, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.CategoryId is { } categoryId)
            query = query.Where(p => p.CategoryId == categoryId);

        if (filter.IsActive is { } active)
            query = query.Where(p => p.IsActive == active);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Add(Product product) => context.Products.Add(product);

    public void Remove(Product product) => context.Products.Remove(product);

    public void ExpectVersion(Product product, uint version) =>
        context.Entry(product).Property(p => p.Version).OriginalValue = version;

    private IQueryable<Product> PublicProducts() =>
        context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && (p.CategoryId == null || p.Category!.IsActive));
}
