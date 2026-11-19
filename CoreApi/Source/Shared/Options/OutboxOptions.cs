namespace LittleEcommerce.Source.Shared.Options;

public class OutboxOptions
{
    public const string Section = "Outbox";

    public bool DispatcherEnabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 20;

    public int MaxAttempts { get; set; } = 5;

    public int LeaseSeconds { get; set; } = 120;

    public int BaseRetryDelaySeconds { get; set; } = 30;

    public int MaxRetryDelayMinutes { get; set; } = 60;
}
