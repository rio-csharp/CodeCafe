using System.Text;

using CodeCafe.Application.Common.Markdown;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.ExportPage;

// Page file conventions on top of the shared block renderer: "# title" is the page and content
// headings get Level + 1 hashes (the importer's matching headingLevelOffset round-trips the
// level).
internal static class MarkdownPageExporter
{
    public static string Render(Page page, IReadOnlyList<Block> blocks)
    {
        var output = new StringBuilder().Append("# ").AppendLine(MarkdownBlockRenderer.EscapeText(page.Title)).AppendLine();
        MarkdownBlockRenderer.RenderBlocks(output, page, blocks, headingHashOffset: 1);
        return output.ToString().TrimEnd() + Environment.NewLine;
    }
}
