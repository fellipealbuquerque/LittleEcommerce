namespace LittleEcommerce.Source.Shared.Options;

public class JwtOptions
{
    public const string Section = "Jwt";
    public const int MinimumSecretLength = 32;

    public string Issuer { get; set; } = "little-ecommerce";

    public string Audience { get; set; } = "little-ecommerce-clients";

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    public int CleanupIntervalMinutes { get; set; } = 60;
}
