using LittleEcommerce.Source.Common.Abstractions;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Infrastructure.Mongo;
using LittleEcommerce.Source.Shared.Extensions;
using LittleEcommerce.Source.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MongoDB.Bson;

namespace LittleEcommerce.Source.Infrastructure.Auditing;

public static class EntityChangeCollector
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";

    private static readonly HashSet<string> IgnoredProperties = [nameof(BaseEntity.CreatedAt), nameof(BaseEntity.UpdatedAt), nameof(Product.Version)];

    public static List<AuditEntry> Collect(DbContext context, AuditActor actor, string? correlationId, DateTime now)
    {
        var entries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (entry.Metadata.ClrType.IsDefined(typeof(NotAuditedAttribute), inherit: true))
                continue;

            var changes = Changes(entry);

            if (entry.State == EntityState.Modified && changes.ElementCount == 0)
                continue;

            entries.Add(new AuditEntry
            {
                Timestamp = now,
                Kind = AuditKinds.EntityChange,
                Action = ActionOf(entry),
                Actor = actor,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = KeyOf(entry),
                Changes = changes,
                CorrelationId = correlationId
            });
        }

        return entries;
    }

    private static BsonDocument Changes(EntityEntry entry)
    {
        var changes = new BsonDocument();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            if (IgnoredProperties.Contains(name))
                continue;

            var sensitive = SensitiveData.IsSensitive(name);
            var key = name.ToCamelCase();

            switch (entry.State)
            {
                case EntityState.Added when property.CurrentValue is not null:
                    changes[key] = new BsonDocument { ["new"] = Value(property.CurrentValue, sensitive) };
                    break;
                case EntityState.Deleted:
                    changes[key] = new BsonDocument { ["old"] = Value(property.OriginalValue, sensitive) };
                    break;
                case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                    changes[key] = new BsonDocument
                    {
                        ["old"] = Value(property.OriginalValue, sensitive),
                        ["new"] = Value(property.CurrentValue, sensitive)
                    };
                    break;
            }
        }

        return changes;
    }

    private static string ActionOf(EntityEntry entry) => entry.State switch
    {
        EntityState.Added => Created,
        EntityState.Deleted => Deleted,
        _ when entry.Entity is ISoftDeletable && WasSoftDeleted(entry) => Deleted,
        _ => Updated
    };

    private static bool WasSoftDeleted(EntityEntry entry)
    {
        var deletedAt = entry.Property(nameof(ISoftDeletable.DeletedAt));
        return deletedAt.OriginalValue is null && deletedAt.CurrentValue is not null;
    }

    private static string KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();

        return key is null
            ? string.Empty
            : string.Join(':', key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()));
    }

    private static BsonValue Value(object? value, bool sensitive) =>
        sensitive && value is not null ? SensitiveData.Mask : BsonValues.From(value);
}
