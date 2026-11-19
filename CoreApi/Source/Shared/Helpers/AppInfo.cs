using System.Reflection;

namespace LittleEcommerce.Source.Shared.Helpers;

public static class AppInfo
{
    private static readonly Assembly Assembly = typeof(Program).Assembly;

    public static string Name { get; } = Assembly.GetName().Name ?? "LittleEcommerce";

    public static string Version { get; } =
        (Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];
}
