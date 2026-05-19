using System.Globalization;
using System.Text;

namespace CodeCafe.Infrastructure.Notes;

public static class SlugGenerator
{
    public static string FromTitle(string title, string fallback)
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

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? fallback : slug;
    }

    public static string WithSuffix(string baseSlug, int suffix)
    {
        return suffix == 0
            ? baseSlug
            : $"{baseSlug}-{Random.Shared.Next(0x1000, 0xffff):x4}";
    }
}
