using System.Text.Json;
using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Infrastructure.Auditing;
using LittleEcommerce.Source.Shared.Serialization;

namespace LittleEcommerce.Source.Infrastructure.Outbox;

public sealed class OutboxWriter(IOutboxRepository repository, ICurrentActor actor, TimeProvider clock) : IOutbox
{
    public Guid Enqueue<TPayload>(string type, TPayload payload, string aggregateType, string aggregateId)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        var message = new OutboxMessage
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload, JsonDefaults.Options),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            CreatedAt = now,
            NextAttemptAt = now,
            CorrelationId = actor.CorrelationId
        };

        repository.Add(message);

        return message.Id;
    }
}
