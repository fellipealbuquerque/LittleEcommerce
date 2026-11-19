using MongoDB.Bson;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public static class AuditKinds
{
    public const string EntityChange = "entity-change";
    public const string AdminAction = "admin-action";
    public const string AccessDenied = "access-denied";
}

public static class AuditActorTypes
{
    public const string User = "user";
    public const string Anonymous = "anonymous";
    public const string System = "system";
}

public sealed class AuditEntry
{
    public ObjectId Id { get; set; }

    public DateTime Timestamp { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public AuditActor? Actor { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public BsonDocument? Changes { get; set; }

    public AuditRequest? Request { get; set; }

    public string? Permission { get; set; }

    public int? StatusCode { get; set; }

    public bool? Succeeded { get; set; }

    public double? DurationMs { get; set; }

    public string? CorrelationId { get; set; }
}

public sealed class AuditActor
{
    public string Type { get; set; } = AuditActorTypes.System;

    public string? UserId { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }
}

public sealed class AuditRequest
{
    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string? QueryString { get; set; }

    public BsonDocument? Arguments { get; set; }
}
