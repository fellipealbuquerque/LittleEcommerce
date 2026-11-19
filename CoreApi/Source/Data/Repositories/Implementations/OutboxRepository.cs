using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.Filters;
using Microsoft.EntityFrameworkCore;

namespace LittleEcommerce.Source.Data.Repositories.Implementations;

public class OutboxRepository(ApplicationDbContext context) : IOutboxRepository
{
    public void Add(OutboxMessage message) => context.OutboxMessages.Add(message);

    public async Task<IReadOnlyList<OutboxMessage>> ClaimDueAsync(DateTime now, TimeSpan lease, int batchSize, CancellationToken ct)
    {
        var pending = nameof(OutboxStatus.Pending);
        var processing = nameof(OutboxStatus.Processing);

        var due = await context.OutboxMessages
            .FromSql($"""
                SELECT * FROM outbox_messages
                WHERE (status = {pending} AND next_attempt_at <= {now})
                   OR (status = {processing} AND locked_until < {now})
                ORDER BY next_attempt_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var message in due)
        {
            message.Status = OutboxStatus.Processing;
            message.Attempts++;
            message.LockedUntil = now + lease;
        }

        return due;
    }

    public Task<OutboxMessage?> GetAsync(Guid id, CancellationToken ct) =>
        context.OutboxMessages.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<(IReadOnlyList<OutboxMessage> Items, int Total)> ListAsync(OutboxFilter filter, CancellationToken ct)
    {
        var query = context.OutboxMessages.AsNoTracking();

        if (filter.Status is { } status)
            query = query.Where(m => m.Status == status);

        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(m => m.Type == filter.Type);

        if (!string.IsNullOrWhiteSpace(filter.AggregateId))
            query = query.Where(m => m.AggregateId == filter.AggregateId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyDictionary<OutboxStatus, int>> CountByStatusAsync(CancellationToken ct) =>
        await context.OutboxMessages
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

    public Task<int> RequeueDeadLettersAsync(string? type, DateTime now, CancellationToken ct) =>
        context.OutboxMessages
            .Where(m => m.Status == OutboxStatus.DeadLetter && (type == null || m.Type == type))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxStatus.Pending)
                .SetProperty(m => m.Attempts, 0)
                .SetProperty(m => m.NextAttemptAt, now)
                .SetProperty(m => m.LockedUntil, (DateTime?)null), ct);
}
