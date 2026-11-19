namespace LittleEcommerce.Source.Shared.Options;

public class AuditLoggingOptions
{
    public const string Section = "AuditLogging";

    public bool Enabled { get; set; } = true;

    public string Collection { get; set; } = "audit_logs";

    public int RetentionDays { get; set; } = 365;

    public int BatchSize { get; set; } = 100;

    public int QueueCapacity { get; set; } = 50_000;
}
