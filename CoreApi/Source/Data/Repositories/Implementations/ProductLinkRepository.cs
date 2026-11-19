using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class ProductLinkRepository(ApplicationDbContext context) : IProductLinkRepository
{
    public async Task<IReadOnlyList<ProductLink>> ListAsync(Guid productId, ProductLinkType? type, bool onlyPublic, CancellationToken ct)
    {
        var query = context.ProductLinks
            .AsNoTracking()
            .Include(l => l.LinkedProduct)
            .ThenInclude(p => p.Category)
            .Where(l => l.ProductId == productId);

        if (type is { } linkType)
            query = query.Where(l => l.Type == linkType);

        if (onlyPublic)
            query = query.Where(l => l.LinkedProduct.IsActive && (l.LinkedProduct.CategoryId == null || l.LinkedProduct.Category!.IsActive));

        return await query
            .OrderBy(l => l.Type)
            .ThenBy(l => l.Position)
            .ThenBy(l => l.LinkedProduct.Name)
            .ToListAsync(ct);
    }

    public Task<ProductLink?> GetAsync(Guid productId, Guid linkId, CancellationToken ct) =>
        context.ProductLinks
            .Include(l => l.LinkedProduct)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(l => l.ProductId == productId && l.Id == linkId, ct);

    public Task<ProductLink?> FindAsync(Guid productId, Guid linkedProductId, ProductLinkType type, CancellationToken ct) =>
        context.ProductLinks.FirstOrDefaultAsync(l => l.ProductId == productId && l.LinkedProductId == linkedProductId && l.Type == type, ct);

    public async Task<IReadOnlyList<ProductLink>> ListTouchingAsync(Guid productId, CancellationToken ct) =>
        await context.ProductLinks
            .Where(l => l.ProductId == productId || l.LinkedProductId == productId)
            .ToListAsync(ct);

    public void Add(ProductLink link) => context.ProductLinks.Add(link);

    public void Remove(ProductLink link) => context.ProductLinks.Remove(link);
}
