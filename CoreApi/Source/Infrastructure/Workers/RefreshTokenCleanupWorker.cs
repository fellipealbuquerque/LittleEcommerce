using LittleEcommerce.Source.Data.Repositories.Interfaces;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;

namespace LittleEcommerce.Source.Infrastructure.Workers;

public class RefreshTokenCleanupWorker(
    IServiceScopeFactory scopes,
    IOptions<JwtOptions> options,
    TimeProvider clock,
    ILogger<RefreshTokenCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.CleanupIntervalMinutes));
        using var timer = new PeriodicTimer(interval, clock);

        do
        {
            await CleanupAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task<int> CleanupAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            var removed = await repository.DeleteExpiredAsync(clock.GetUtcNow().UtcDateTime, ct);

            if (removed > 0)
                logger.LogInformation("Limpeza removeu {Count} refresh tokens expirados", removed);

            return removed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha na limpeza de refresh tokens expirados");
            return 0;
        }
    }
}
