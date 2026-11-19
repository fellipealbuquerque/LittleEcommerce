using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Shared.Filters;

public class ProductFilter : BaseFilter, IValidatableObject
{
    [StringLength(100, ErrorMessage = "Busca muito longa")]
    public string? Search { get; set; }

    public string? Category { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "minPrice não pode ser negativo")]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "maxPrice não pode ser negativo")]
    public decimal? MaxPrice { get; set; }

    public bool? InStock { get; set; }

    public ProductSort Sort { get; set; } = ProductSort.Newest;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice is { } min && MaxPrice is { } max && min > max)
            yield return new ValidationResult("minPrice não pode ser maior que maxPrice", [nameof(MinPrice)]);
    }
}

public class AdminProductFilter : BaseFilter
{
    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public bool? IsActive { get; set; }
}
