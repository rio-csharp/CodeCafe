using CodeCafe.Application.Common.Markdown;
using Markdig.Syntax;

namespace CodeCafe.Application.Notebooks.ImportNotebook;

// Notebook file conventions on top of the shared markdown core: "# title" names the notebook
// (the last one wins), "## title" starts a page, and content before the first "##" belongs to
// no page. Content headings start at ###, so blocks parse with a level offset of 2 — the
// exporter emits Level + 2 hashes, and the offset round-trips the level.
internal static class MarkdownNotebookParser
{
    private const int HeadingLevelOffset = 2;

    public static ParsedNotebook Parse(string fileName, string markdown)
    {
        var document = MarkdownDocumentParser.ParseDocument(markdown);
        var title = Path.GetFileNameWithoutExtension(fileName).Trim();
        var pages = new List<ParsedPage>();
        ParsedPage? currentPage = null;
        var content = new List<Block>();

        void FlushPage()
        {
            if (currentPage is null)
            {
                return;
            }

            foreach (var block in MarkdownDocumentParser.ParseBlocks(content, HeadingLevelOffset))
            {
                currentPage.Blocks.Add(block);
            }

            content.Clear();
        }

        foreach (var block in document)
        {
            switch (block)
            {
                case HeadingBlock { IsSetext: false, Level: 1 } h1:
                    title = MarkdownInlineParser.ParseSingleLine(h1.Inline).ToPlainText().Trim();
                    break;
                case HeadingBlock { IsSetext: false, Level: 2 } h2:
                    FlushPage();
                    currentPage = new ParsedPage(MarkdownInlineParser.ParseSingleLine(h2.Inline).ToPlainText().Trim());
                    pages.Add(currentPage);
                    break;
                default:
                    if (currentPage is not null)
                    {
                        content.Add(block);
                    }

                    break;
            }
        }

        FlushPage();
        return new ParsedNotebook(title, pages);
    }
}

internal sealed record ParsedNotebook(string Title, IReadOnlyList<ParsedPage> Pages);
