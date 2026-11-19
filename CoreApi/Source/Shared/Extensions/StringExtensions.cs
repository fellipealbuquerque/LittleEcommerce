using System.Text.Json;

namespace LittleEcommerce.Source.Shared.Extensions;

public static class StringExtensions
{
    public static string ToCamelCase(this string value) => JsonNamingPolicy.CamelCase.ConvertName(value);
}
