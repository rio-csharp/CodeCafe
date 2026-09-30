namespace CodeCafe.Application.Pages.SearchAllPages;

// Builds the excerpt shown for a block-content match: ±60 chars around the first occurrence of
// the query, whitespace collapsed, ellipses where text was cut. Whitespace is collapsed before
// locating the match so the snippet never renders stray newlines; if the query no longer occurs
// after collapsing (it contained whitespace itself), the excerpt falls back to the text's start.
internal static class SearchSnippet
{
    private const int ContextRadius = 60;

    public static string Build(string plainText, string query)
    {
        var collapsed = string.Join(' ', plainText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var index = collapsed.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return collapsed.Length <= 2 * ContextRadius ? collapsed : collapsed[..(2 * ContextRadius)] + '…';
        }

        var start = Math.Max(0, index - ContextRadius);
        var end = Math.Min(collapsed.Length, index + query.Length + ContextRadius);
        var snippet = collapsed[start..end];
        if (start > 0)
        {
            snippet = '…' + snippet;
        }

        if (end < collapsed.Length)
        {
            snippet += '…';
        }

        return snippet;
    }
}
