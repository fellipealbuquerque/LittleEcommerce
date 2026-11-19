using MailKit.Security;

namespace LittleEcommerce.Source.Shared.Options;

public class EmailOptions
{
    public const string Section = "Email";

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "LittleEcommerce";

    public string StoreUrl { get; set; } = "http://localhost:3000";

    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTlsWhenAvailable;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public int TimeoutSeconds { get; set; } = 15;
}
