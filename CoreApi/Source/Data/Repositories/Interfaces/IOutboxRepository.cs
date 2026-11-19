using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Data.Repositories.Interfaces;

public interface IOutboxRepository
{
    void Add(OutboxMessage message);

    Task<IReadOnlyList<OutboxMessage>> ClaimDueAsync(DateTime now, TimeSpan lease, int batchSize, CancellationToken ct);

    Task<OutboxMessage?> GetAsync(Guid id, CancellationToken ct);

    Task<(IReadOnlyList<OutboxMessage> Items, int Total)> ListAsync(OutboxFilter filter, CancellationToken ct);

    Task<IReadOnlyDictionary<OutboxStatus, int>> CountByStatusAsync(CancellationToken ct);

    Task<int> RequeueDeadLettersAsync(string? type, DateTime now, CancellationToken ct);
}
