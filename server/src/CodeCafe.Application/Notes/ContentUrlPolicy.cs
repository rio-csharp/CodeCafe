namespace CodeCafe.Application.Notes;

/// <summary>
/// Decides which URLs may be stored inside TipTap content. Only inert URLs are kept so a
/// stored document cannot carry active-content schemes (javascript:, data:, vbscript:,
/// protocol-relative hosts, ...) past the frontend's render-time sanitization.
/// </summary>
public static class ContentUrlPolicy
{
    /// <summary>Allows http(s), mailto:, tel:, and root-relative paths. For link hrefs.</summary>
    public static bool IsAllowedLinkUrl(string? url) => IsAllowed(url, allowMailSchemes: true);

    /// <summary>Allows http(s) and root-relative paths. For image/embed src attributes.</summary>
    public static bool IsAllowedResourceUrl(string? url) => IsAllowed(url, allowMailSchemes: false);

    private static bool IsAllowed(string? url, bool allowMailSchemes)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            // Empty or whitespace-only URLs are inert and left untouched.
            return true;
        }

        var trimmed = url.TrimStart();
        if (trimmed.StartsWith('/'))
        {
            // Root-relative paths are fine; protocol-relative //host is not.
            return !trimmed.StartsWith("//", StringComparison.Ordinal);
        }

        return trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || (allowMailSchemes
                && (trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)));
    }
}
