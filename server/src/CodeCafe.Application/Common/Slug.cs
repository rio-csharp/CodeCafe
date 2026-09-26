using System.Text;

namespace CodeCafe.Application.Common;

public static class Slug
{
    public static string Normalize(string slug) => slug.Trim().ToLowerInvariant();

    // Unicode letters stay (CJK titles produce readable slugs); everything else becomes a hyphen.
    public static string GenerateFromTitle(string title, int maxLength, string fallback)
    {
        var builder = new StringBuilder(title.Length);
        var lastWasDash = true; // suppresses a leading dash
        foreach (var character in Normalize(title))
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().TrimEnd('-');
        if (slug.Length > maxLength)
        {
            slug = slug[..maxLength].TrimEnd('-');
        }

        return slug.Length == 0 ? fallback : slug;
    }

    public static bool IsValid(string slug, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > maxLength)
        {
            return false;
        }

        if (slug.StartsWith('-') || slug.EndsWith('-') || slug.Contains("--", StringComparison.Ordinal))
        {
            return false;
        }

        return slug.All(character => char.IsLetterOrDigit(character) || character == '-');
    }

    // The stored column caps the slug, so the base gives up as many characters as the suffix needs.
    public static string WithSuffix(string baseSlug, string suffix, int maxLength)
    {
        var separator = $"-{suffix}";
        var room = maxLength - separator.Length;
        var trimmed = baseSlug.Length > room ? baseSlug[..room].TrimEnd('-') : baseSlug;

        return $"{trimmed}{separator}";
    }
}
