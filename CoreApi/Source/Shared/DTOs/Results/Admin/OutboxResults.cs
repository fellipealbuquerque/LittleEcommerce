using System.Text.Json;
using LittleEcommerce.Source.Common.Enums;

namespace LittleEcommerce.Source.Shared.DTOs.Results.Admin;

public record OutboxMessageResult(
    Guid Id,
    string Type,
    OutboxStatus Status,
    string AggregateType,
    string AggregateId,
    int Attempts,
    DateTime CreatedAt,
    DateTime NextAttemptAt,
    DateTime? ProcessedAt,
    string? LastError,
    string? CorrelationId,
    JsonElement Payload
);

public record OutboxSummaryResult(
    int Pending,
    int Processing,
    int Sent,
    int DeadLetter
);

public record OutboxRequeueResult(int Requeued);

public record OutboxEnqueuedResult(Guid MessageId);
