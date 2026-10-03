using System.Text;

using CodeCafe.Application.Common.Markdown;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Notebooks.ExportNotebook;

// Notebook file conventions on top of the shared block renderer: "# title" is the notebook,
// "## title" starts a page, and content headings get Level + 2 hashes (the importer's matching
// headingLevelOffset round-trips the level).
internal static class MarkdownExporter
{
    public static string Render(
        Notebook notebook,
        IReadOnlyList<Page> pages,
        IReadOnlyDictionary<Guid, IReadOnlyList<Block>> blocksByPage
    )
    {
        var orderedPages = OrderPages(notebook, pages);
        var output = new StringBuilder().Append("# ").AppendLine(MarkdownBlockRenderer.EscapeText(notebook.Title)).AppendLine();

        foreach (var page in orderedPages)
        {
            output.Append("## ").AppendLine(MarkdownBlockRenderer.EscapeText(page.Title)).AppendLine();
            if (blocksByPage.TryGetValue(page.Id, out var blocks))
            {
                MarkdownBlockRenderer.RenderBlocks(output, page, blocks, headingHashOffset: 2);
            }

            output.AppendLine();
        }

        return output.ToString().TrimEnd() + Environment.NewLine;
    }

    private static IReadOnlyList<Page> OrderPages(Notebook notebook, IReadOnlyList<Page> pages)
    {
        var byId = pages.ToDictionary(page => page.Id);
        var ordered = new List<Page>(pages.Count);
        var pending = new Stack<Page>();
        PushPageChain(pending, byId, notebook.FirstPageId);

        while (pending.Count > 0)
        {
            var page = pending.Pop();
            ordered.Add(page);
            PushPageChain(pending, byId, page.FirstChildId);
        }

        return ordered;
    }

    private static void PushPageChain(Stack<Page> pending, IReadOnlyDictionary<Guid, Page> byId, Guid? head)
    {
        var chain = new List<Page>();
        var visited = new HashSet<Guid>();
        for (var id = head; id is not null && visited.Add(id.Value) && byId.TryGetValue(id.Value, out var page); id = page.NextSiblingId)
        {
            chain.Add(page);
        }

        for (var index = chain.Count - 1; index >= 0; index--)
        {
            pending.Push(chain[index]);
        }
    }
}
