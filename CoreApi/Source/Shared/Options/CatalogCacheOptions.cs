namespace LittleEcommerce.Source.Shared.Options;

public class CatalogCacheOptions
{
    public const string Section = "CatalogCache";

    public bool Enabled { get; set; } = true;

    public int TtlSeconds { get; set; } = 300;
}
