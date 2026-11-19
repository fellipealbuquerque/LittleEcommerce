using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Infrastructure.Outbox;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;

namespace LittleEcommerce.Source.Infrastructure.Workers;

public sealed class OutboxDispatcherWorker(
    IServiceScopeFactory scopes,
    IOutboxSignal signal,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.DispatcherEnabled)
        {
            logger.LogInformation("Dispatcher do outbox desligado (Outbox:DispatcherEnabled = false)");
            return;
        }

        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, settings.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await RunOnceAsync(stoppingToken);

            if (processed >= settings.BatchSize)
                continue;

            await signal.WaitAsync(pollInterval, stoppingToken);
        }
    }

    private async Task<int> RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha no ciclo do dispatcher do outbox");
            return 0;
        }
    }
}
