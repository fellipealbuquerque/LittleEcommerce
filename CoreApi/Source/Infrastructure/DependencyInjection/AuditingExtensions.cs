using LittleEcommerce.Source.Infrastructure.Auditing;
using LittleEcommerce.Source.Shared.Options;
using Microsoft.AspNetCore.Authorization;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class AuditingExtensions
{
    public static IServiceCollection AddAuditing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuditLoggingOptions>(configuration.GetSection(AuditLoggingOptions.Section));

        AuditMongoConventions.Register();

        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentActor, HttpCurrentActor>();

        services.AddSingleton<AuditLogWriter>();
        services.AddSingleton<IAuditWriter>(sp => sp.GetRequiredService<AuditLogWriter>());
        services.AddHostedService(sp => sp.GetRequiredService<AuditLogWriter>());

        services.AddScoped<AuditBuffer>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<AuditTransactionInterceptor>();
        services.AddScoped<AdminAuditFilter>();
        services.AddScoped<IAuditLogReader, MongoAuditLogReader>();

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditingAuthorizationResultHandler>();

        return services;
    }
}
