using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Shared.Helpers;

namespace LittleEcommerce.Source.Shared.DTOs.Requests.Catalog;

public static class CatalogValidation
{
    public const string SkuPattern = "^[A-Za-z0-9][A-Za-z0-9-]{2,49}$";
    public const string SkuMessage = "SKU com 3 a 50 caracteres: letras, números e hífen";
    public const string SlugMessage = "Slug só com letras minúsculas, números e hífen";
    public const double MaxPrice = 9_999_999_999.99;
}

public record CreateProductRequest(
    [Required(ErrorMessage = "Informe o SKU")]
    [RegularExpression(CatalogValidation.SkuPattern, ErrorMessage = CatalogValidation.SkuMessage)]
    string Sku,
    [Required(ErrorMessage = "Informe o nome")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 200 caracteres")]
    string Name,
    [RegularExpression(Slugifier.Pattern, ErrorMessage = CatalogValidation.SlugMessage)]
    [StringLength(220)]
    string? Slug,
    [StringLength(5000, ErrorMessage = "Descrição muito longa")]
    string? Description,
    [Range(0.01, CatalogValidation.MaxPrice, ErrorMessage = "O preço precisa ser maior que zero")]
    decimal Price,
    [Range(0.01, CatalogValidation.MaxPrice, ErrorMessage = "O preço de comparação precisa ser maior que zero")]
    decimal? CompareAtPrice,
    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo")]
    int Stock,
    Guid? CategoryId,
    [Url(ErrorMessage = "URL de imagem inválida")]
    [StringLength(500)]
    string? ImageUrl,
    bool IsActive = true
);

public record UpdateProductRequest(
    [Required(ErrorMessage = "Informe o nome")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 200 caracteres")]
    string Name,
    [RegularExpression(Slugifier.Pattern, ErrorMessage = CatalogValidation.SlugMessage)]
    [StringLength(220)]
    string? Slug,
    [StringLength(5000, ErrorMessage = "Descrição muito longa")]
    string? Description,
    [Range(0.01, CatalogValidation.MaxPrice, ErrorMessage = "O preço precisa ser maior que zero")]
    decimal Price,
    [Range(0.01, CatalogValidation.MaxPrice, ErrorMessage = "O preço de comparação precisa ser maior que zero")]
    decimal? CompareAtPrice,
    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo")]
    int Stock,
    Guid? CategoryId,
    [Url(ErrorMessage = "URL de imagem inválida")]
    [StringLength(500)]
    string? ImageUrl,
    bool IsActive,
    [Required(ErrorMessage = "Informe a versão lida do produto")]
    uint? Version
);

public record UpdateProductStatusRequest(bool IsActive);

public record UpdateStockRequest(
    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo")]
    int Quantity
);

public record CreateProductLinkRequest(
    [Required(ErrorMessage = "Informe o produto vinculado")]
    Guid? LinkedProductId,
    [Required(ErrorMessage = "Informe o tipo de vínculo")]
    ProductLinkType? Type,
    [Range(1, 999, ErrorMessage = "A quantidade precisa estar entre 1 e 999")]
    int Quantity = 1,
    [Range(0, 9999, ErrorMessage = "A posição precisa estar entre 0 e 9999")]
    int Position = 0
);

public record CreateCategoryRequest(
    [Required(ErrorMessage = "Informe o nome")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 100 caracteres")]
    string Name,
    [RegularExpression(Slugifier.Pattern, ErrorMessage = CatalogValidation.SlugMessage)]
    [StringLength(120)]
    string? Slug,
    [StringLength(500, ErrorMessage = "Descrição muito longa")]
    string? Description,
    bool IsActive = true
);

public record UpdateCategoryRequest(
    [Required(ErrorMessage = "Informe o nome")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome precisa ter de 2 a 100 caracteres")]
    string Name,
    [RegularExpression(Slugifier.Pattern, ErrorMessage = CatalogValidation.SlugMessage)]
    [StringLength(120)]
    string? Slug,
    [StringLength(500, ErrorMessage = "Descrição muito longa")]
    string? Description,
    bool IsActive
);
