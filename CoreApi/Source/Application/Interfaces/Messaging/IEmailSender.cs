using LittleEcommerce.Source.Application.Abstractions;

namespace LittleEcommerce.Source.Application.Interfaces.Messaging;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
