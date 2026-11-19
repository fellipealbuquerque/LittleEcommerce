using System.ComponentModel.DataAnnotations;

namespace LittleEcommerce.Source.Data.Entities;

public class Role : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
