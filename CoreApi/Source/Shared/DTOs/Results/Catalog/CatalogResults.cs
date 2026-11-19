using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Shared.DTOs.Results.Catalog;

public record CategoryRef(Guid Id, string Name, string Slug);

public record ProductSummaryResult(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    decimal Price,
    decimal? CompareAtPrice,
    int? DiscountPercent,
    bool InStock,
    int Stock,
    string? ImageUrl,
    CategoryRef? Category
);

public record ProductResult(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? CompareAtPrice,
    int? DiscountPercent,
    bool InStock,
    int Stock,
    string? ImageUrl,
    CategoryRef? Category,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record AdminProductResult(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    bool IsActive,
    string? ImageUrl,
    CategoryRef? Category,
    uint Version,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CategoryResult(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    int Products
);

public record ProductLinkResult(
    Guid Id,
    ProductLinkType Type,
    int Quantity,
    int Position,
    ProductSummaryResult Product
);

public record CatalogPage<T>(IReadOnlyList<T> Items, int Total);
