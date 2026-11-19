using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace LittleEcommerce.Source.Infrastructure.HealthChecks;

public class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await redis.GetDatabase().PingAsync().WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy($"ping {latency.TotalMilliseconds:F1} ms");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis indisponível", ex);
        }
    }
}
