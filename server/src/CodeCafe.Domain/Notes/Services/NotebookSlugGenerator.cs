using System.Globalization;
using System.Text;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Notes.Services;

public static class NotebookSlugGenerator
{
    public const int MaxSlugLength = NotebookSlug.MaxLength;

    public const int MinSlugLength = NotebookSlug.MinLength;

    public static string FromTitle(string title, string fallback)
    {
        return FromTitle(title, fallback, MaxSlugLength);
    }

    public static string FromTitle(string title, string fallback, int maxLength)
    {
        var normalized = title.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousWasDash = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(character);
            if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
            {
                builder.Append(lower);
                previousWasDash = false;
            }
            else if (!previousWasDash && builder.Length > 0)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var slug = Truncate(builder.ToString().Trim('-'), maxLength);
        return string.IsNullOrWhiteSpace(slug) ? Truncate(fallback, maxLength) : slug;
    }

    public static string WithSuffix(string baseSlug, int suffix)
    {
        return WithSuffix(baseSlug, suffix, MaxSlugLength);
    }

    public static string WithSuffix(string baseSlug, int suffix, int maxLength)
    {
        if (suffix == 0)
        {
            return Truncate(baseSlug, maxLength);
        }

        var suffixText = $"-{suffix}";
        var head = Truncate(baseSlug, Math.Max(0, maxLength - suffixText.Length));
        return head.Length == 0
            ? Truncate(suffix.ToString(CultureInfo.InvariantCulture), maxLength)
            : head + suffixText;
    }

    public static string WithUniqueSuffix(string baseSlug, int maxLength)
    {
        var unique = Guid.NewGuid().ToString("N");
        if (maxLength <= unique.Length)
        {
            return Truncate(unique, maxLength);
        }

        // Reserve at least half the budget for the random tail so the base slug cannot crowd it out.
        var reservedForUnique = Math.Min(
            unique.Length + 1,
            Math.Max(maxLength / 2, unique.Length + 1)
        );
        var head = Truncate(baseSlug, maxLength - reservedForUnique);
        return head.Length == 0
            ? Truncate(unique, maxLength)
            : $"{head}-{Truncate(unique, maxLength - head.Length - 1)}";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (maxLength <= 0)
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength].TrimEnd('-');
    }
}
