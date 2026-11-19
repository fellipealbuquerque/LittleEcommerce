namespace LittleEcommerce.Source.Shared.Options;

public class MongoLoggingOptions
{
    public const string Section = "MongoLogging";

    public bool Enabled { get; set; } = true;

    public string Collection { get; set; } = "logs";

    public int RetentionDays { get; set; } = 30;

    public int BatchSize { get; set; } = 200;

    public int FlushIntervalSeconds { get; set; } = 2;

    public int QueueLimit { get; set; } = 50_000;
}
