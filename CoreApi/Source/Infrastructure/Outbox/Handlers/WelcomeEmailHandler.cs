using System.Text.Json;
using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Common.Constants;
using LittleEcommerce.Source.Data.Entities;
using LittleEcommerce.Source.Infrastructure.Email;
using LittleEcommerce.Source.Shared.Options;
using LittleEcommerce.Source.Shared.Serialization;
using Microsoft.Extensions.Options;

namespace LittleEcommerce.Source.Infrastructure.Outbox.Handlers;

public sealed class WelcomeEmailHandler(IEmailSender sender, IOptions<EmailOptions> options) : IOutboxMessageHandler
{
    public string Type => OutboxMessageTypes.WelcomeEmail;

    public Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<WelcomeEmailPayload>(message.Payload, JsonDefaults.Options)
                      ?? throw new InvalidOperationException($"Payload vazio na mensagem {message.Id}");

        var email = WelcomeEmailTemplate.Render(payload, options.Value.StoreUrl, $"{message.Id}@little-ecommerce");

        return sender.SendAsync(email, ct);
    }
}
