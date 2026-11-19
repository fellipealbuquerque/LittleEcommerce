namespace LittleEcommerce.Source.Shared.Helpers;

public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
