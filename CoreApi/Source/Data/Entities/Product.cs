using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Abstractions;

namespace LittleEcommerce.Source.Data.Entities;

public class Product : BaseEntity, ISoftDeletable
{
    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public uint Version { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ProductLink> Links { get; set; } = new List<ProductLink>();
}
