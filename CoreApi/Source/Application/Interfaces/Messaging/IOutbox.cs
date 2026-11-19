namespace LittleEcommerce.Source.Application.Interfaces.Messaging;

public interface IOutbox
{
    Guid Enqueue<TPayload>(string type, TPayload payload, string aggregateType, string aggregateId);
}

public interface IOutboxSignal
{
    void Notify();

    Task WaitAsync(TimeSpan timeout, CancellationToken ct);
}
