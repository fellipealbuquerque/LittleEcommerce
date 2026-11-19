using Microsoft.AspNetCore.WebUtilities;

namespace LittleEcommerce.Source.Shared.Helpers;

public static class SensitiveData
{
    public const string Mask = "***";

    private static readonly string[] Keywords =
    [
        "password",
        "passwd",
        "senha",
        "secret",
        "token",
        "authorization",
        "cookie",
        "apikey",
        "cardnumber",
        "creditcard",
        "cvv",
        "cvc"
    ];

    public static bool IsSensitive(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = name.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        return Keywords.Any(normalized.Contains);
    }

    public static string MaskQueryString(QueryString queryString)
    {
        if (!queryString.HasValue)
            return string.Empty;

        var parsed = QueryHelpers.ParseQuery(queryString.Value);

        if (!parsed.Keys.Any(IsSensitive))
            return queryString.Value!;

        var pairs = parsed.SelectMany(entry => entry.Value.Select(value =>
            new KeyValuePair<string, string?>(entry.Key, IsSensitive(entry.Key) ? Mask : value)));

        return QueryString.Create(pairs).Value!;
    }
}
