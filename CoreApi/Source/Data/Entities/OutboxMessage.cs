using System.ComponentModel.DataAnnotations;
using LittleEcommerce.Source.Common.Abstractions;
using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Data.Entities;

[NotAudited]
public class OutboxMessage
{
    public const int LastErrorMaxLength = 2000;

    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = "{}";

    [Required]
    [MaxLength(50)]
    public string AggregateType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AggregateId { get; set; } = string.Empty;

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    public int Attempts { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime NextAttemptAt { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(LastErrorMaxLength)]
    public string? LastError { get; set; }

    [MaxLength(64)]
    public string? CorrelationId { get; set; }
}
