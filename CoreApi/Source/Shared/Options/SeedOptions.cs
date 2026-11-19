namespace LittleEcommerce.Source.Shared.Options;

public class SeedOptions
{
    public const string Section = "Seed";

    public string AdminName { get; set; } = "Administrador";

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;
}
