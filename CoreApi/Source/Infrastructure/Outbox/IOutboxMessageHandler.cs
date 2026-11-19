using LittleEcommerce.Source.Data.Entities;

namespace LittleEcommerce.Source.Infrastructure.Outbox;

public interface IOutboxMessageHandler
{
    string Type { get; }

    Task HandleAsync(OutboxMessage message, CancellationToken ct);
}
