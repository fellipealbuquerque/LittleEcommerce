namespace LittleEcommerce.Source.Application.Abstractions;

public record WelcomeEmailPayload(Guid UserId, string Email, string Name);

public record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string Html,
    string Text,
    string? MessageId = null
);
