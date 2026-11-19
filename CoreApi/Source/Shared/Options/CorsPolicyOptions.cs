namespace LittleEcommerce.Source.Shared.Options;

public class CorsPolicyOptions
{
    public const string Section = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
