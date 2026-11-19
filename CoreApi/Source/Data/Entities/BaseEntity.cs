using System.ComponentModel.DataAnnotations;

namespace LittleEcommerce.Source.Data.Entities;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
