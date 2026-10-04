namespace CodeCafe.Infrastructure.Persistence;

// User input in substring searches is literal text: \, % and _ are LIKE metacharacters and
// must be escaped, with the escape character passed explicitly to EF.Functions.ILike —
// its default (ESCAPE '') would treat them as wildcards.
public static class LikePatterns
{
    public const string EscapeCharacter = "\\";

    public static string Substring(string value) => $"%{Escape(value)}%";

    private static string Escape(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
