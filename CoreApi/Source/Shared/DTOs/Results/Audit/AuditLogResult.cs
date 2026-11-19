using System.Text.Json;

namespace LittleEcommerce.Source.Shared.DTOs.Results.Audit;

public record AuditLogResult(
    string Id,
    DateTime Timestamp,
    string Kind,
    string Action,
    AuditActorResult? Actor,
    string? EntityType,
    string? EntityId,
    JsonElement? Changes,
    AuditRequestResult? Request,
    string? Permission,
    int? StatusCode,
    bool? Succeeded,
    double? DurationMs,
    string? CorrelationId
);

public record AuditActorResult(
    string Type,
    string? UserId,
    string? Email,
    string? Role,
    string? Ip,
    string? UserAgent
);

public record AuditRequestResult(
    string Method,
    string Path,
    string? QueryString,
    JsonElement? Arguments
);
