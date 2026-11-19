using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Abstractions;

namespace LittleEcommerce.Source.Data.Entities;

[NotAudited]
public class RefreshToken
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public Guid FamilyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(40)]
    public string? RevokedReason { get; set; }

    public Guid? ReplacedById { get; set; }

    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }
}
