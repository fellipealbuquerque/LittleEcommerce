using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LittleEcommerce.Source.Common.Enums;
using LittleEcommerce.Source.Shared.Filters;

namespace LittleEcommerce.Source.Application.Caching;

public static class CatalogCacheKeys
{
    public const string Categories = "categories";

    public static string ProductList(ProductFilter filter)
    {
        var normalized = string.Join('|',
            filter.Search?.Trim().ToLowerInvariant() ?? string.Empty,
            filter.Category?.Trim().ToLowerInvariant() ?? string.Empty,
            filter.MinPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            filter.MaxPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            filter.InStock?.ToString() ?? string.Empty,
            filter.Sort.ToString(),
            filter.Limit.ToString(CultureInfo.InvariantCulture),
            filter.Offset.ToString(CultureInfo.InvariantCulture));

        return $"products:list:{Hash(normalized)}";
    }

    public static string Product(string idOrSlug) => $"products:detail:{idOrSlug.Trim().ToLowerInvariant()}";

    public static string Links(string idOrSlug, ProductLinkType? type) =>
        $"products:links:{idOrSlug.Trim().ToLowerInvariant()}:{type?.ToString() ?? "all"}";

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}
