using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Abstractions;

namespace LittleEcommerce.Source.Data.Entities;

[NotAudited]
public class CartItem
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid CartId { get; set; }

    public Cart Cart { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPriceWhenAdded { get; set; }

    public DateTime AddedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
