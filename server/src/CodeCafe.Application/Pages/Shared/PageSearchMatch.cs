using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Shared;

// One search hit as the repository sees it: the page, its notebook's slug and title, and the
// PlainText of the first matching block (null when only the page title matched).
public sealed record PageSearchMatch(
    Page Page,
    string NotebookSlug,
    string NotebookTitle,
    string? MatchedBlockPlainText);
