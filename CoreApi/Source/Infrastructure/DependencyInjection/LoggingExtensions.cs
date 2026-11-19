using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Infrastructure.Logging;
using LittleEcommerce.Source.Shared.Helpers;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class LoggingExtensions
{
    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    private const string RequestTemplate =
        "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0} ms";

    private static readonly string[] QuietPaths = ["/health", "/swagger", "/scalar", "/openapi", "/favicon.ico"];

    public static IServiceCollection AddStructuredLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoLoggingOptions>(configuration.GetSection(MongoLoggingOptions.Section));

        services.AddSerilog((sp, logger) =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var mongo = sp.GetRequiredService<IOptions<MongoLoggingOptions>>().Value;

            logger
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", AppInfo.Name)
                .Enrich.WithProperty("Version", AppInfo.Version)
                .Enrich.WithProperty("Environment", environment.EnvironmentName)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.With<SensitiveDataMaskingEnricher>()
                .WriteTo.Console(outputTemplate: ConsoleTemplate);

            if (!mongo.Enabled)
                return;

            logger.WriteTo.Sink(
                new MongoLogSink(sp.GetRequiredService<IMongoDatabase>(), mongo),
                new BatchingOptions
                {
                    BatchSizeLimit = mongo.BatchSize,
                    BufferingTimeLimit = TimeSpan.FromSeconds(mongo.FlushIntervalSeconds),
                    QueueLimit = mongo.QueueLimit,
                    EagerlyEmitFirstEvent = true
                });
        });

        return services;
    }

    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = RequestTemplate;
            options.GetLevel = RequestLevel;
            options.EnrichDiagnosticContext = (diagnostic, http) =>
            {
                diagnostic.Set(LogProperties.QueryString, SensitiveData.MaskQueryString(http.Request.QueryString));
                diagnostic.Set(LogProperties.ClientIp, http.Connection.RemoteIpAddress?.ToString());
                diagnostic.Set(LogProperties.UserAgent, http.Request.Headers.UserAgent.ToString());
                diagnostic.Set(LogProperties.Endpoint, http.GetEndpoint()?.DisplayName);

                var userId = http.User.FindFirst("sub")?.Value;
                if (userId is not null)
                    diagnostic.Set(LogProperties.UserId, userId);
            };
        });

        return app;
    }

    internal static LogEventLevel RequestLevel(HttpContext http, double elapsedMs, Exception? exception)
    {
        if (exception is not null || http.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        if (http.Response.StatusCode >= StatusCodes.Status400BadRequest)
            return LogEventLevel.Warning;

        return QuietPaths.Any(p => http.Request.Path.StartsWithSegments(p))
            ? LogEventLevel.Debug
            : LogEventLevel.Information;
    }
}
