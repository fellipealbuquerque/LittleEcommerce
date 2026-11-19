using LittleEcommerce.Source.Shared.DTOs.Results.Admin;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Application.Interfaces.Services;

public interface IOutboxAdminService
{
    Task<(IReadOnlyList<OutboxMessageResult> Items, int Total)> ListAsync(OutboxFilter filter, CancellationToken ct = default);

    Task<OutboxSummaryResult> SummaryAsync(CancellationToken ct = default);

    Task<OutboxMessageResult> GetAsync(Guid id, CancellationToken ct = default);

    Task<OutboxMessageResult> RetryAsync(Guid id, CancellationToken ct = default);

    Task<OutboxRequeueResult> RetryDeadLettersAsync(string? type, CancellationToken ct = default);
}
