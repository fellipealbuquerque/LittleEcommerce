using System.Text.Json;
using LittleEcommerce.Source.Shared.DTOs.Results.Audit;
using LittleEcommerce.Source.Shared.Filters;
using LittleEcommerce.Source.Shared.Options;
using LittleEcommerce.Source.Shared.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public interface IAuditLogReader
{
    Task<(IReadOnlyList<AuditLogResult> Items, long Total)> ListAsync(AuditLogFilter filter, CancellationToken ct = default);
}

public sealed class MongoAuditLogReader(IMongoDatabase database, IOptions<AuditLoggingOptions> options) : IAuditLogReader
{
    private readonly IMongoCollection<AuditEntry> _collection = database.GetCollection<AuditEntry>(options.Value.Collection);

    public async Task<(IReadOnlyList<AuditLogResult> Items, long Total)> ListAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = BuildFilter(filter);

        var total = await _collection.CountDocumentsAsync(query, cancellationToken: ct);

        var entries = await _collection.Find(query)
            .SortByDescending(e => e.Timestamp)
            .ThenByDescending(e => e.Id)
            .Skip(filter.Offset)
            .Limit(filter.Limit)
            .ToListAsync(ct);

        return (entries.Select(ToResult).ToList(), total);
    }

    internal static FilterDefinition<AuditEntry> BuildFilter(AuditLogFilter filter)
    {
        var builder = Builders<AuditEntry>.Filter;
        var filters = new List<FilterDefinition<AuditEntry>>();

        if (!string.IsNullOrWhiteSpace(filter.Kind))
            filters.Add(builder.Eq(e => e.Kind, filter.Kind));

        if (!string.IsNullOrWhiteSpace(filter.Action))
            filters.Add(builder.Eq(e => e.Action, filter.Action));

        if (!string.IsNullOrWhiteSpace(filter.ActorId))
            filters.Add(builder.Eq(e => e.Actor!.UserId, filter.ActorId));

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            filters.Add(builder.Eq(e => e.EntityType, filter.EntityType));

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            filters.Add(builder.Eq(e => e.EntityId, filter.EntityId));

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
            filters.Add(builder.Eq(e => e.CorrelationId, filter.CorrelationId));

        if (filter.From is { } from)
            filters.Add(builder.Gte(e => e.Timestamp, from.ToUniversalTime()));

        if (filter.To is { } to)
            filters.Add(builder.Lte(e => e.Timestamp, to.ToUniversalTime()));

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }

    private static AuditLogResult ToResult(AuditEntry entry) => new(
        entry.Id.ToString(),
        entry.Timestamp,
        entry.Kind,
        entry.Action,
        entry.Actor is { } actor
            ? new AuditActorResult(actor.Type, actor.UserId, actor.Email, actor.Role, actor.Ip, actor.UserAgent)
            : null,
        entry.EntityType,
        entry.EntityId,
        ToJson(entry.Changes),
        entry.Request is { } request
            ? new AuditRequestResult(request.Method, request.Path, request.QueryString, ToJson(request.Arguments))
            : null,
        entry.Permission,
        entry.StatusCode,
        entry.Succeeded,
        entry.DurationMs,
        entry.CorrelationId);

    private static JsonElement? ToJson(BsonDocument? document) =>
        document is null
            ? null
            : JsonSerializer.SerializeToElement(BsonTypeMapper.MapToDotNetValue(document), JsonDefaults.Options);
}
