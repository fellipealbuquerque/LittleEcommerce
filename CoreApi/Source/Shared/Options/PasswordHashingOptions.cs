namespace LittleEcommerce.Source.Shared.Options;

public class PasswordHashingOptions
{
    public const string Section = "PasswordHashing";

    public int WorkFactor { get; set; } = 11;
}
