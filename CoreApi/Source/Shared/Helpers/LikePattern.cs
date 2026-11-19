namespace LittleEcommerce.Source.Shared.Helpers;

public static class LikePattern
{
    public const string EscapeCharacter = "\\";

    public static string Contains(string term) =>
        $"%{term.Trim().Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter).Replace("%", "\\%").Replace("_", "\\_")}%";
}
