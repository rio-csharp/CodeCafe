using System.Text.Json;

using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Notebooks;

// Notebook-level concerns of the exporter: the # / ## skeleton and the page-chain walk. Block
// rendering itself is covered through MarkdownPageExporterTests (shared renderer).
public sealed class MarkdownExporterTests
{
    [Fact]
    public void Render_EmitsNotebookAndPageTitles()
    {
        var notebook = NewNotebook();
        var pages = new List<Page>();
        var page = AddPage(notebook, pages, null, "First", "first");

        var markdown = MarkdownExporter.Render(notebook, pages, new Dictionary<Guid, IReadOnlyList<Block>>()).ReplaceLineEndings("\n");

        Assert.Equal("# Notebook\n\n## First\n", markdown);
    }

    [Fact]
    public void Render_WalksThePageChain_DepthFirst()
    {
        var notebook = NewNotebook();
        var pages = new List<Page>();
        var a = AddPage(notebook, pages, null, "A", "a");
        var b = AddPage(notebook, pages, null, "B", "b");
        var child = AddPage(notebook, pages, a, "Child", "child");

        var markdown = MarkdownExporter.Render(notebook, pages, new Dictionary<Guid, IReadOnlyList<Block>>()).ReplaceLineEndings("\n");

        var aIndex = markdown.IndexOf("## A", StringComparison.Ordinal);
        var childIndex = markdown.IndexOf("## Child", StringComparison.Ordinal);
        var bIndex = markdown.IndexOf("## B", StringComparison.Ordinal);
        Assert.True(aIndex >= 0 && childIndex > aIndex && bIndex > childIndex, markdown);
    }

    [Fact]
    public void Render_PageOutsideTheChain_IsSkipped()
    {
        var notebook = NewNotebook();
        var pages = new List<Page>();
        var linked = AddPage(notebook, pages, null, "Linked", "linked");
        // Never linked into the notebook chain: a stale row must not leak into the export.
        var orphan = Page.Create(notebook.Id, null, "Orphan", "orphan", "z");
        pages.Add(orphan);

        var markdown = MarkdownExporter.Render(notebook, pages, new Dictionary<Guid, IReadOnlyList<Block>>()).ReplaceLineEndings("\n");

        Assert.DoesNotContain("Orphan", markdown);
    }

    [Fact]
    public void Render_ChainCycle_Terminates()
    {
        var notebook = NewNotebook();
        var pages = new List<Page>();
        var a = AddPage(notebook, pages, null, "A", "a");
        var b = AddPage(notebook, pages, null, "B", "b");
        // Corrupt the chain into a cycle; the visited set keeps the walk finite.
        b.SetNextSibling(a.Id);

        var markdown = MarkdownExporter.Render(notebook, pages, new Dictionary<Guid, IReadOnlyList<Block>>()).ReplaceLineEndings("\n");

        Assert.Equal(1, CountOccurrences(markdown, "## A\n"));
        Assert.Equal(1, CountOccurrences(markdown, "## B\n"));
    }

    [Fact]
    public void Render_Headings_GetLevelPlusTwoHashes()
    {
        var notebook = NewNotebook();
        var pages = new List<Page>();
        var page = AddPage(notebook, pages, null, "P", "p");
        var blocks = new List<Block>();
        AddBlock(page, blocks, BlockTypes.Heading, """{"level":1,"spans":[{"text":"H","marks":[]}]}""");

        var markdown = MarkdownExporter.Render(
            notebook,
            pages,
            new Dictionary<Guid, IReadOnlyList<Block>> { [page.Id] = blocks }
        ).ReplaceLineEndings("\n");

        Assert.Contains("\n### H\n", markdown);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = 0; (index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0; index += needle.Length)
        {
            count++;
        }

        return count;
    }

    private static Notebook NewNotebook()
        => Notebook.Create(Guid.CreateVersion7(), "Notebook", null, "notebook", NotebookVisibility.Private);

    private static Page AddPage(Notebook notebook, List<Page> all, Page? parent, string title, string slug)
    {
        var siblings = all.Where(page => page.ParentId == parent?.Id).ToList();
        var last = siblings.Count > 0 ? siblings[^1] : null;
        var page = Page.Create(notebook.Id, parent?.Id, title, slug, SiblingSortKeys.KeyForInsert(siblings, siblings.Count));
        PageChain.Link(page, notebook, parent, last);
        all.Add(page);
        return page;
    }

    private static void AddBlock(Page page, List<Block> all, string type, string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var normalized = BlockPayloads.ValidateAndNormalize(type, document.RootElement);
        Assert.True(normalized.IsSuccess, normalized.Error?.Message);
        var block = Block.Create(
            page.Id,
            null,
            type,
            normalized.Value!.CanonicalJson,
            normalized.Value.PlainText,
            BlockSiblingSortKeys.KeyForInsert([], 0)
        );
        BlockChain.Insert(block, page, null, null, null);
        all.Add(block);
    }
}
