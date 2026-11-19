using System.Globalization;
using System.Text;

namespace LittleEcommerce.Source.Shared.Helpers;

public static class Slugifier
{
    public const string Pattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

    public static string Slugify(string value, int maxLength = 200)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasDash = true;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            var lower = char.ToLowerInvariant(character);

            if (char.IsAsciiLetterOrDigit(lower))
            {
                builder.Append(lower);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');

        if (slug.Length > maxLength)
            slug = slug[..maxLength].TrimEnd('-');

        return slug.Length == 0 ? "item" : slug;
    }
}
