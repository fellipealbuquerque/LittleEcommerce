using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Abstractions;
using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Data.Entities;

public class User : BaseEntity, ISoftDeletable
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public int SessionVersion { get; set; } = 1;

    public DateTime? LastLoginAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public void InvalidateSessions() => SessionVersion++;
}
