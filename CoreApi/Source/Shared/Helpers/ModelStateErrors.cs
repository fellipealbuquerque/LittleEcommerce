using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LittleEcommerce.Source.Shared.Helpers;

public static class ModelStateErrors
{
    public const string InvalidJsonMessage = "JSON inválido ou com valor de tipo incorreto";
    public const string InvalidValueMessage = "Valor inválido";
    public const string BodyKey = "body";

    private const string JsonPathRoot = "$";

    public static Dictionary<string, string[]> From(ModelStateDictionary modelState)
    {
        var invalid = modelState.Where(entry => entry.Value is { Errors.Count: > 0 }).ToList();
        var jsonErrors = invalid.Where(entry => entry.Key.StartsWith(JsonPathRoot, StringComparison.Ordinal)).ToList();

        if (jsonErrors.Count > 0)
            return jsonErrors
                .GroupBy(entry => FromJsonPath(entry.Key))
                .ToDictionary(group => group.Key, _ => new[] { InvalidJsonMessage });

        return invalid
            .GroupBy(entry => CamelCasePath(entry.Key))
            .ToDictionary(
                group => group.Key,
                group => group
                    .SelectMany(entry => entry.Value!.Errors)
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? InvalidValueMessage : error.ErrorMessage)
                    .ToArray());
    }

    internal static string CamelCasePath(string key) =>
        string.Join('.', key.Split('.').Select(segment =>
            segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..]));

    private static string FromJsonPath(string key) =>
        key == JsonPathRoot ? BodyKey : CamelCasePath(key[(JsonPathRoot.Length + 1)..]);
}
