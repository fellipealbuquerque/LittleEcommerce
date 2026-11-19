using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Caching;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Mappers;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Catalog;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using LittleEcommerce.Source.Shared.Filters;
using LittleEcommerce.Source.Shared.Helpers;

namespace LittleEcommerce.Source.Application.Services;

public class ProductAdminService(
    IUnitOfWork unitOfWork,
    IProductRepository products,
    ICategoryRepository categories,
    IProductLinkRepository links,
    ICatalogCache cache,
    TimeProvider clock,
    ILogger<ProductAdminService> logger) : IProductAdminService
{
    private const int MaxSlugAttempts = 50;

    public async Task<(IReadOnlyList<AdminProductResult> Items, int Total)> ListAsync(AdminProductFilter filter, CancellationToken ct = default)
    {
        var (items, total) = await products.ListAdminAsync(filter, ct);
        return (items.Select(p => p.ToAdmin()).ToList(), total);
    }

    public async Task<AdminProductResult> GetAsync(Guid id, CancellationToken ct = default) =>
        (await FindAsync(id, ct)).ToAdmin();

    public async Task<AdminProductResult> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await products.SkuExistsAsync(sku, null, ct))
            throw new ConflictException($"Já existe um produto com o SKU {sku}", ErrorCodes.SkuAlreadyExists);

        ValidatePrices(request.Price, request.CompareAtPrice);

        var product = new Product
        {
            Sku = sku,
            Name = request.Name.Trim(),
            Slug = await ResolveSlugAsync(request.Slug, request.Name, null, ct),
            Description = request.Description?.Trim(),
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            Stock = request.Stock,
            IsActive = request.IsActive,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            Category = await ResolveCategoryAsync(request.CategoryId, ct)
        };

        products.Add(product);
        await SaveAndInvalidateAsync(ct);

        logger.LogInformation("Produto {ProductId} ({Sku}) criado", product.Id, product.Sku);

        return product.ToAdmin();
    }

    public async Task<AdminProductResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await FindAsync(id, ct);
        products.ExpectVersion(product, request.Version!.Value);

        ValidatePrices(request.Price, request.CompareAtPrice);

        product.Name = request.Name.Trim();
        product.Slug = request.Slug is null ? product.Slug : await ResolveSlugAsync(request.Slug, request.Name, product.Id, ct);
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.Stock = request.Stock;
        product.IsActive = request.IsActive;
        product.ImageUrl = request.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.Category = await ResolveCategoryAsync(request.CategoryId, ct);

        await SaveAndInvalidateAsync(ct);

        return product.ToAdmin();
    }

    public async Task<AdminProductResult> SetStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var product = await FindAsync(id, ct);
        product.IsActive = isActive;
        await SaveAndInvalidateAsync(ct);
        return product.ToAdmin();
    }

    public async Task<AdminProductResult> SetStockAsync(Guid id, int quantity, CancellationToken ct = default)
    {
        var product = await FindAsync(id, ct);
        product.Stock = quantity;
        await SaveAndInvalidateAsync(ct);
        return product.ToAdmin();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await FindAsync(id, ct);

        foreach (var link in await links.ListTouchingAsync(product.Id, ct))
            links.Remove(link);

        products.Remove(product);
        await SaveAndInvalidateAsync(ct);

        logger.LogInformation("Produto {ProductId} ({Sku}) removido", product.Id, product.Sku);
    }

    public async Task<IReadOnlyList<ProductLinkResult>> ListLinksAsync(Guid id, CancellationToken ct = default)
    {
        await FindAsync(id, ct);
        return (await links.ListAsync(id, null, onlyPublic: false, ct)).Select(l => l.ToResult()).ToList();
    }

    public async Task<ProductLinkResult> AddLinkAsync(Guid id, CreateProductLinkRequest request, CancellationToken ct = default)
    {
        var product = await FindAsync(id, ct);
        var linkedId = request.LinkedProductId!.Value;
        var type = request.Type!.Value;

        if (linkedId == product.Id)
            throw new ValidationException("linkedProductId", "Um produto não pode ser vinculado a ele mesmo");

        var linked = await products.GetAsync(linkedId, ct)
                     ?? throw new ValidationException("linkedProductId", "Produto vinculado não encontrado");

        if (await links.FindAsync(product.Id, linked.Id, type, ct) is not null)
            throw new ConflictException("Esse vínculo já existe", ErrorCodes.LinkAlreadyExists);

        var now = clock.GetUtcNow().UtcDateTime;
        var quantity = type.HasQuantity() ? request.Quantity : 1;

        var link = new ProductLink
        {
            ProductId = product.Id,
            LinkedProductId = linked.Id,
            LinkedProduct = linked,
            Type = type,
            Quantity = quantity,
            Position = request.Position,
            CreatedAt = now
        };

        links.Add(link);

        if (type.IsSymmetric() && await links.FindAsync(linked.Id, product.Id, type, ct) is null)
        {
            links.Add(new ProductLink
            {
                ProductId = linked.Id,
                LinkedProductId = product.Id,
                Type = type,
                Quantity = 1,
                Position = request.Position,
                CreatedAt = now
            });
        }

        await SaveAndInvalidateAsync(ct);

        logger.LogInformation("Vínculo {Type} entre {ProductId} e {LinkedProductId} criado", type, product.Id, linked.Id);

        return link.ToResult();
    }

    public async Task RemoveLinkAsync(Guid id, Guid linkId, CancellationToken ct = default)
    {
        var link = await links.GetAsync(id, linkId, ct)
                   ?? throw new NotFoundException("Vínculo não encontrado");

        links.Remove(link);

        if (link.Type.IsSymmetric() && await links.FindAsync(link.LinkedProductId, link.ProductId, link.Type, ct) is { } reverse)
            links.Remove(reverse);

        await SaveAndInvalidateAsync(ct);
    }

    private async Task<Product> FindAsync(Guid id, CancellationToken ct) =>
        await products.GetAsync(id, ct) ?? throw new NotFoundException("Produto não encontrado");

    private async Task<Category?> ResolveCategoryAsync(Guid? categoryId, CancellationToken ct)
    {
        if (categoryId is not { } id)
            return null;

        return await categories.GetAsync(id, ct)
               ?? throw new ValidationException("categoryId", "Categoria não encontrada");
    }

    private async Task<string> ResolveSlugAsync(string? requested, string name, Guid? productId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (await products.SlugExistsAsync(requested, productId, ct))
                throw new ConflictException($"Já existe um produto com o slug {requested}", ErrorCodes.SlugAlreadyExists);

            return requested;
        }

        var baseSlug = Slugifier.Slugify(name);

        for (var attempt = 1; attempt <= MaxSlugAttempts; attempt++)
        {
            var candidate = attempt == 1 ? baseSlug : $"{baseSlug}-{attempt}";

            if (!await products.SlugExistsAsync(candidate, productId, ct))
                return candidate;
        }

        return $"{baseSlug}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    private static void ValidatePrices(decimal price, decimal? compareAtPrice)
    {
        if (decimal.Round(price, 2) != price)
            throw new ValidationException("price", "Use no máximo duas casas decimais");

        if (compareAtPrice is { } original && decimal.Round(original, 2) != original)
            throw new ValidationException("compareAtPrice", "Use no máximo duas casas decimais");

        if (compareAtPrice is { } compare && compare <= price)
            throw new ValidationException("compareAtPrice", "O preço \"de\" precisa ser maior que o preço de venda");
    }

    private async Task SaveAndInvalidateAsync(CancellationToken ct)
    {
        await unitOfWork.SaveChangesAsync(ct);
        await cache.InvalidateAsync();
    }
}
