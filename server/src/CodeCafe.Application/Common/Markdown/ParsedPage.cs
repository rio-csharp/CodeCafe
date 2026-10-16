namespace CodeCafe.Application.Common.Markdown;

// One parsed page: its title and top-level blocks (nesting lives on ParsedMarkdownBlock.Children).
internal sealed record ParsedPage(string Title)
{
    public List<ParsedMarkdownBlock> Blocks { get; } = [];
}
