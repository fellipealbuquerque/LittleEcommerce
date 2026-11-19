using LittleEcommerce.Source.Infrastructure.HealthChecks;
using LittleEcommerce.Source.Shared.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class HealthExtensions
{
    public const string LiveTag = "live";
    public const string ReadyTag = "ready";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public static IServiceCollection AddHealth(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag])
            .AddCheck<PostgresHealthCheck>("postgres", HealthStatus.Unhealthy, [ReadyTag], Timeout)
            .AddCheck<RedisHealthCheck>("redis", HealthStatus.Unhealthy, [ReadyTag], Timeout)
            .AddCheck<MongoHealthCheck>("mongo", HealthStatus.Unhealthy, [ReadyTag], Timeout);

        return services;
    }

    public static WebApplication MapHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(LiveTag),
            ResponseWriter = WriteAsync
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(ReadyTag),
            ResponseWriter = WriteAsync
        });

        return app;
    }

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var body = new HealthResult(
            report.Status.ToString(),
            Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            report.Entries
                .Select(e => new HealthEntryResult(
                    e.Key,
                    e.Value.Status.ToString(),
                    Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                    e.Value.Description))
                .ToList());

        return context.Response.WriteAsJsonAsync(body, JsonDefaults.Options);
    }

    private sealed record HealthResult(string Status, double DurationMs, IReadOnlyList<HealthEntryResult> Checks);

    private sealed record HealthEntryResult(string Name, string Status, double DurationMs, string? Description);
}
