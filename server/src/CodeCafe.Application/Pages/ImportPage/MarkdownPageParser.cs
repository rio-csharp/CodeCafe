using CodeCafe.Application.Common.Markdown;
using Markdig.Syntax;

namespace CodeCafe.Application.Pages.ImportPage;

// Page file conventions on top of the shared markdown core: the first "# title" names the page
// (falling back to the file name) and everything else is content — including later "#" lines,
// which stay as content headings clamped to level 1. Content headings start at ##, so blocks
// parse with a level offset of 1 — the exporter emits Level + 1 hashes, and the offset
// round-trips the level.
internal static class MarkdownPageParser
{
    private const int HeadingLevelOffset = 1;

    public static ParsedPage Parse(string fileName, string markdown)
    {
        var document = MarkdownDocumentParser.ParseDocument(markdown);
        var title = string.Empty;
        var content = new List<Block>();
        foreach (var block in document)
        {
            if (block is HeadingBlock { IsSetext: false, Level: 1 } h1 && title.Length == 0)
            {
                title = MarkdownInlineParser.ParseSingleLine(h1.Inline).ToPlainText().Trim();
                continue;
            }

            content.Add(block);
        }

        if (title.Length == 0)
        {
            title = Path.GetFileNameWithoutExtension(fileName).Trim();
        }

        var page = new ParsedPage(title);
        page.Blocks.AddRange(MarkdownDocumentParser.ParseBlocks(content, HeadingLevelOffset));
        return page;
    }
}
