namespace CodeCafe.Application.Pages;

// Paths are chains of page slugs ("/setup/rust-notes"); they are derived from the parent chain,
// never stored, so moves and renames leave no path to rewrite.
public static class PagePath
{
    public static IReadOnlyList<string> Parse(string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(PageSlug.Normalize).ToList();

    public static string Build(IEnumerable<string> segments) => "/" + string.Join('/', segments);
}
