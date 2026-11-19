using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Application.Interfaces.Services;
using LittleEcommerce.Source.Application.Services;
using LittleEcommerce.Source.Infrastructure.Email;
using LittleEcommerce.Source.Infrastructure.Outbox;
using LittleEcommerce.Source.Infrastructure.Outbox.Handlers;
using LittleEcommerce.Source.Infrastructure.Workers;
using LittleEcommerce.Source.Shared.Options;

namespace LittleEcommerce.Source.Infrastructure.DependencyInjection;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.Section))
            .Validate(o => !string.IsNullOrWhiteSpace(o.FromAddress), "Email:FromAddress não configurado. Defina Email__FromAddress.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Smtp.Host), "Email:Smtp:Host não configurado. Defina Email__Smtp__Host.")
            .ValidateOnStart();

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.Section));

        services.AddSingleton<IOutboxSignal, OutboxSignal>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddScoped<OutboxDispatcher>();
        services.AddScoped<IOutboxMessageHandler, WelcomeEmailHandler>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IOutboxAdminService, OutboxAdminService>();

        services.AddHostedService<OutboxDispatcherWorker>();

        return services;
    }
}
