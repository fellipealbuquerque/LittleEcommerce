using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Caching;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Mappers;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Requests.Catalog;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;
using LittleEcommerce.Source.Shared.Helpers;

namespace LittleEcommerce.Source.Application.Services;

public class CategoryAdminService(
    IUnitOfWork unitOfWork,
    ICategoryRepository categories,
    ICatalogCache cache) : ICategoryAdminService
{
    public async Task<IReadOnlyList<CategoryResult>> ListAsync(CancellationToken ct = default) =>
        (await categories.ListAdminAsync(ct)).Select(c => c.Category.ToResult(c.Products)).ToList();

    public async Task<CategoryResult> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = await ResolveSlugAsync(request.Slug, request.Name, null, ct),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };

        categories.Add(category);
        await SaveAndInvalidateAsync(ct);

        return category.ToResult(0);
    }

    public async Task<CategoryResult> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await FindAsync(id, ct);

        category.Name = request.Name.Trim();
        category.Slug = request.Slug is null ? category.Slug : await ResolveSlugAsync(request.Slug, request.Name, category.Id, ct);
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;

        await SaveAndInvalidateAsync(ct);

        return category.ToResult(await categories.CountProductsAsync(id, ct));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await FindAsync(id, ct);

        if (await categories.HasProductsAsync(id, ct))
            throw new ConflictException("A categoria ainda tem produtos; mova ou remova os produtos antes", ErrorCodes.CategoryInUse);

        categories.Remove(category);
        await SaveAndInvalidateAsync(ct);
    }

    private async Task<Category> FindAsync(Guid id, CancellationToken ct) =>
        await categories.GetAsync(id, ct) ?? throw new NotFoundException("Categoria não encontrada");

    private async Task<string> ResolveSlugAsync(string? requested, string name, Guid? categoryId, CancellationToken ct)
    {
        var slug = string.IsNullOrWhiteSpace(requested) ? Slugifier.Slugify(name, 100) : requested;

        if (await categories.SlugExistsAsync(slug, categoryId, ct))
            throw new ConflictException($"Já existe uma categoria com o slug {slug}", ErrorCodes.SlugAlreadyExists);

        return slug;
    }

    private async Task SaveAndInvalidateAsync(CancellationToken ct)
    {
        await unitOfWork.SaveChangesAsync(ct);
        await cache.InvalidateAsync();
    }
}
