using System.Text.Json;
using LittleEcommerce.Source.Application.Exceptions;
using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Common.Errors;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.DTOs.Results.Admin;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Application.Services;

public class OutboxAdminService(
    IUnitOfWork unitOfWork,
    IOutboxRepository repository,
    IOutboxSignal signal,
    TimeProvider clock,
    ILogger<OutboxAdminService> logger) : IOutboxAdminService
{
    public async Task<(IReadOnlyList<OutboxMessageResult> Items, int Total)> ListAsync(OutboxFilter filter, CancellationToken ct = default)
    {
        var (items, total) = await repository.ListAsync(filter, ct);
        return (items.Select(ToResult).ToList(), total);
    }

    public async Task<OutboxSummaryResult> SummaryAsync(CancellationToken ct = default)
    {
        var counts = await repository.CountByStatusAsync(ct);

        return new OutboxSummaryResult(
            counts.GetValueOrDefault(OutboxStatus.Pending),
            counts.GetValueOrDefault(OutboxStatus.Processing),
            counts.GetValueOrDefault(OutboxStatus.Sent),
            counts.GetValueOrDefault(OutboxStatus.DeadLetter));
    }

    public async Task<OutboxMessageResult> GetAsync(Guid id, CancellationToken ct = default) =>
        ToResult(await FindAsync(id, ct));

    public async Task<OutboxMessageResult> RetryAsync(Guid id, CancellationToken ct = default)
    {
        var message = await FindAsync(id, ct);

        if (message.Status == OutboxStatus.Processing)
            throw new ConflictException("A mensagem está sendo enviada agora; tente de novo em instantes", ErrorCodes.OutboxMessageProcessing);

        message.Status = OutboxStatus.Pending;
        message.Attempts = 0;
        message.NextAttemptAt = clock.GetUtcNow().UtcDateTime;
        message.LockedUntil = null;

        await unitOfWork.SaveChangesAsync(ct);
        signal.Notify();

        logger.LogInformation("Mensagem {MessageId} ({Type}) colocada de volta na fila", message.Id, message.Type);

        return ToResult(message);
    }

    public async Task<OutboxRequeueResult> RetryDeadLettersAsync(string? type, CancellationToken ct = default)
    {
        var requeued = await repository.RequeueDeadLettersAsync(string.IsNullOrWhiteSpace(type) ? null : type, clock.GetUtcNow().UtcDateTime, ct);

        if (requeued > 0)
            signal.Notify();

        logger.LogInformation("{Count} mensagens em dead-letter voltaram para a fila (tipo: {Type})", requeued, type ?? "todos");

        return new OutboxRequeueResult(requeued);
    }

    private async Task<OutboxMessage> FindAsync(Guid id, CancellationToken ct) =>
        await repository.GetAsync(id, ct) ?? throw new NotFoundException("Mensagem não encontrada");

    private static OutboxMessageResult ToResult(OutboxMessage message)
    {
        using var payload = JsonDocument.Parse(message.Payload);

        return new OutboxMessageResult(
            message.Id,
            message.Type,
            message.Status,
            message.AggregateType,
            message.AggregateId,
            message.Attempts,
            message.CreatedAt,
            message.NextAttemptAt,
            message.ProcessedAt,
            message.LastError,
            message.CorrelationId,
            payload.RootElement.Clone());
    }
}
