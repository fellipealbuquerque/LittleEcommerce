namespace LittleEcommerce.Source.Shared.Extensions;

public static class ConfigurationExtensions
{
    public static string RequiredConnectionString(this IConfiguration configuration, string name)
    {
        var value = configuration.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Connection string '{name}' não configurada. Defina ConnectionStrings__{name}.");

        return value;
    }
}
