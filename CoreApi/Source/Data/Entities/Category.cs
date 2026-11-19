using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Abstractions;

namespace LittleEcommerce.Source.Data.Entities;

public class Category : BaseEntity, ISoftDeletable
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
