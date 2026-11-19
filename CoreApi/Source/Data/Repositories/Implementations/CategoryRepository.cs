using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    public Task<Category?> GetAsync(Guid id, CancellationToken ct) =>
        context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Category?> FindPublicAsync(string idOrSlug, CancellationToken ct)
    {
        var query = context.Categories.AsNoTracking().Where(c => c.IsActive);

        return Guid.TryParse(idOrSlug, out var id)
            ? query.FirstOrDefaultAsync(c => c.Id == id, ct)
            : query.FirstOrDefaultAsync(c => c.Slug == idOrSlug.ToLowerInvariant(), ct);
    }

    public async Task<IReadOnlyList<(Category Category, int Products)>> ListPublicAsync(CancellationToken ct)
    {
        var rows = await context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new { Category = c, Products = c.Products.Count(p => p.IsActive) })
            .ToListAsync(ct);

        return rows.Select(r => (r.Category, r.Products)).ToList();
    }

    public async Task<IReadOnlyList<(Category Category, int Products)>> ListAdminAsync(CancellationToken ct)
    {
        var rows = await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { Category = c, Products = c.Products.Count() })
            .ToListAsync(ct);

        return rows.Select(r => (r.Category, r.Products)).ToList();
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct) =>
        context.Categories.AnyAsync(c => c.Slug == slug && c.Id != exceptId, ct);

    public Task<bool> HasProductsAsync(Guid id, CancellationToken ct) =>
        context.Products.AnyAsync(p => p.CategoryId == id, ct);

    public Task<int> CountProductsAsync(Guid id, CancellationToken ct) =>
        context.Products.CountAsync(p => p.CategoryId == id, ct);

    public void Add(Category category) => context.Categories.Add(category);

    public void Remove(Category category) => context.Categories.Remove(category);
}
