using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.DTOs.Results.Catalog;

namespace LittleEcommerce.Source.Application.Mappers;

public static class CatalogMapper
{
    public static ProductSummaryResult ToSummary(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Slug,
        product.Price,
        product.CompareAtPrice,
        DiscountPercent(product),
        product.Stock > 0,
        product.Stock,
        product.ImageUrl,
        product.Category.ToRef());

    public static ProductResult ToResult(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Slug,
        product.Description,
        product.Price,
        product.CompareAtPrice,
        DiscountPercent(product),
        product.Stock > 0,
        product.Stock,
        product.ImageUrl,
        product.Category.ToRef(),
        product.CreatedAt,
        product.UpdatedAt);

    public static AdminProductResult ToAdmin(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Slug,
        product.Description,
        product.Price,
        product.CompareAtPrice,
        product.Stock,
        product.IsActive,
        product.ImageUrl,
        product.Category.ToRef(),
        product.Version,
        product.CreatedAt,
        product.UpdatedAt);

    public static CategoryResult ToResult(this Category category, int products) =>
        new(category.Id, category.Name, category.Slug, category.Description, category.IsActive, products);

    public static ProductLinkResult ToResult(this ProductLink link) =>
        new(link.Id, link.Type, link.Quantity, link.Position, link.LinkedProduct.ToSummary());

    public static CategoryRef? ToRef(this Category? category) =>
        category is null ? null : new CategoryRef(category.Id, category.Name, category.Slug);

    internal static int? DiscountPercent(Product product) =>
        product.CompareAtPrice is { } original && original > product.Price
            ? (int)Math.Round((1 - product.Price / original) * 100, MidpointRounding.AwayFromZero)
            : null;
}
