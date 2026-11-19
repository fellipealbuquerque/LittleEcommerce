namespace LittleEcommerce.Source.Shared.Options;

public class RateLimitingOptions
{
    public const string Section = "RateLimiting";
    public const string AuthPolicy = "auth";

    public int AuthPermitLimit { get; set; } = 10;

    public int AuthWindowSeconds { get; set; } = 60;
}
