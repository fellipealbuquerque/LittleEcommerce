using LittleEcommerce.Source.Application.Abstractions;
using LittleEcommerce.Source.Application.Interfaces.Messaging;
using LittleEcommerce.Source.Shared.Options;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LittleEcommerce.Source.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var email = options.Value;
        var smtp = email.Smtp;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(email.FromName, email.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;

        if (message.MessageId is not null)
            mime.MessageId = message.MessageId;

        mime.Body = new BodyBuilder { HtmlBody = message.Html, TextBody = message.Text }.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = (int)TimeSpan.FromSeconds(Math.Max(1, smtp.TimeoutSeconds)).TotalMilliseconds;

        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, ct);

        if (!string.IsNullOrEmpty(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, ct);

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("E-mail {Subject} enviado para {To} via {Host}:{Port}", message.Subject, message.ToAddress, smtp.Host, smtp.Port);
    }
}
