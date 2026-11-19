using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace LittleEcommerce.Source.Infrastructure.HealthChecks;

public class PostgresHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "PostgreSQL indisponível", ex);
        }
    }
}
