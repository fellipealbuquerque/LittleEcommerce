using LittleEcommerce.Source.Application.Interfaces;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace LittleEcommerce.Source.Infrastructure.Outbox;

public sealed class OutboxDispatcher(
    IUnitOfWork unitOfWork,
    IOutboxRepository repository,
    IEnumerable<IOutboxMessageHandler> handlers,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxDispatcher> logger)
{
    private readonly Dictionary<string, IOutboxMessageHandler> _handlers = handlers.ToDictionary(h => h.Type);

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<int> DispatchAsync(CancellationToken ct)
    {
        var settings = options.Value;
        var now = Now;

        var claimed = await unitOfWork.ExecuteAsync(
            () => repository.ClaimDueAsync(now, TimeSpan.FromSeconds(Math.Max(10, settings.LeaseSeconds)), Math.Max(1, settings.BatchSize), ct),
            ct);

        foreach (var message in claimed)
            await ProcessAsync(message, settings, ct);

        return claimed.Count;
    }

    internal static TimeSpan RetryDelay(int attempts, OutboxOptions settings)
    {
        var baseDelay = TimeSpan.FromSeconds(Math.Max(1, settings.BaseRetryDelaySeconds));
        var max = TimeSpan.FromMinutes(Math.Max(1, settings.MaxRetryDelayMinutes));
        var exponent = Math.Clamp(attempts - 1, 0, 20);

        var delay = TimeSpan.FromTicks(baseDelay.Ticks * (1L << exponent));
        return delay > max ? max : delay;
    }

    private async Task ProcessAsync(OutboxMessage message, OutboxOptions settings, CancellationToken ct)
    {
        using var correlation = LogContext.PushProperty(LogProperties.CorrelationId, message.CorrelationId);

        try
        {
            if (!_handlers.TryGetValue(message.Type, out var handler))
                throw new UnknownOutboxMessageTypeException(message.Type);

            await handler.HandleAsync(message, ct);

            message.Status = OutboxStatus.Sent;
            message.ProcessedAt = Now;
            message.LockedUntil = null;
            message.LastError = null;

            logger.LogInformation("Mensagem {MessageId} ({Type}) entregue na tentativa {Attempt}", message.Id, message.Type, message.Attempts);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogInformation("Envio da mensagem {MessageId} interrompido; será retomado quando a reserva expirar", message.Id);
            return;
        }
        catch (Exception ex)
        {
            RegisterFailure(message, ex, settings);
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private void RegisterFailure(OutboxMessage message, Exception exception, OutboxOptions settings)
    {
        message.LockedUntil = null;
        message.LastError = Truncate($"{exception.GetType().Name}: {exception.Message}");

        if (exception is UnknownOutboxMessageTypeException || message.Attempts >= Math.Max(1, settings.MaxAttempts))
        {
            message.Status = OutboxStatus.DeadLetter;
            logger.LogError(exception, "Mensagem {MessageId} ({Type}) foi para dead-letter após {Attempts} tentativas", message.Id, message.Type, message.Attempts);
            return;
        }

        var delay = RetryDelay(message.Attempts, settings);
        message.Status = OutboxStatus.Pending;
        message.NextAttemptAt = Now + delay;

        logger.LogWarning(exception, "Falha na mensagem {MessageId} ({Type}), tentativa {Attempt}; nova tentativa em {Delay}", message.Id, message.Type, message.Attempts, delay);
    }

    private static string Truncate(string value) =>
        value.Length <= OutboxMessage.LastErrorMaxLength ? value : value[..OutboxMessage.LastErrorMaxLength];
}

public sealed class UnknownOutboxMessageTypeException(string type)
    : Exception($"Nenhum handler registrado para o tipo {type}");
