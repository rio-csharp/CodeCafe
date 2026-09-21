using System.Text;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks;

public static class NotebookSlug
{
    public static string Normalize(string slug) => slug.Trim().ToLowerInvariant();

    // Unicode letters stay (CJK titles produce readable slugs); everything else becomes a hyphen.
    public static string GenerateFromTitle(string title)
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
        if (slug.Length > Notebook.MaxSlugLength)
        {
            slug = slug[..Notebook.MaxSlugLength].TrimEnd('-');
        }

        return slug.Length == 0 ? "notebook" : slug;
    }

    public static bool IsValid(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > Notebook.MaxSlugLength)
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
    public static string WithSuffix(string baseSlug, string suffix)
    {
        var separator = $"-{suffix}";
        var room = Notebook.MaxSlugLength - separator.Length;
        var trimmed = baseSlug.Length > room ? baseSlug[..room].TrimEnd('-') : baseSlug;

        return $"{trimmed}{separator}";
    }
}
