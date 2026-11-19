using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Data.Entities;

public class ProductLink
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid LinkedProductId { get; set; }

    public Product LinkedProduct { get; set; } = null!;

    public ProductLinkType Type { get; set; }

    public int Quantity { get; set; } = 1;

    public int Position { get; set; }

    public DateTime CreatedAt { get; set; }
}
