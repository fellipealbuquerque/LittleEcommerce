namespace LittleEcommerce.Source.Shared.Options;

public class DatabaseOptions
{
    public const string Section = "Database";

    public bool MigrateOnStartup { get; set; } = true;
}
