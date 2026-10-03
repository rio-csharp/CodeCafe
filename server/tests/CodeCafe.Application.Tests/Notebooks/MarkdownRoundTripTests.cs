using System.Text.Json;

using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Markdown;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Application.Notebooks.ImportNotebook;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Application.Pages.ImportPage;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Notebooks;

// The superset contract: everything the exporter emits must survive a re-import unchanged.
// Covers the markdown-expressible subset — nested paragraphs deliberately excluded (export
// renders children as indented lines, which markdown cannot represent as structure), as are
// heading levels 5-6 (the +2 offset squashes them at the 6-hash ATX cap) and HTML-rendered
// marks (underline/kbd/sup/sub/abbr lose their marks to tag stripping).
public sealed class MarkdownRoundTripTests
{
    [Fact]
    public void ExportThenImport_RebuildsTheSameBlockTree()
    {
        var notebook = Notebook.Create(Guid.CreateVersion7(), "Notes", null, "notes", NotebookVisibility.Private);
        var page = Page.Create(notebook.Id, null, "Page", "page", "a");
        PageChain.Link(page, notebook, null, null);

        var all = new List<Block>();
        AddBlock(page, all, null, BlockTypes.Heading, """{"level":2,"spans":[{"text":"Section","marks":[]}]}""");
        AddBlock(
            page,
            all,
            null,
            BlockTypes.Paragraph,
            """{"spans":[{"text":"plain ","marks":[]},{"text":"bold","marks":[{"kind":"bold"}]},{"text":" ","marks":[]},{"text":"gone","marks":[{"kind":"strike"}]},{"text":" ","marks":[]},{"text":"docs","marks":[{"kind":"link","href":"https://example.com/docs"}]}]}"""
        );
        var todo = AddBlock(page, all, null, BlockTypes.Todo, """{"checked":false,"spans":[{"text":"parent task","marks":[]}]}""");
        AddBlock(page, all, todo, BlockTypes.Todo, """{"checked":true,"spans":[{"text":"child task","marks":[]}]}""");
        AddBlock(page, all, null, BlockTypes.Code, """{"code":"var x = 1;","language":"csharp"}""");
        AddBlock(page, all, null, BlockTypes.Quote, """{"spans":[{"text":"Wise words","marks":[]}]}""");
        AddBlock(
            page,
            all,
            null,
            BlockTypes.Callout,
            """{"variant":"warning","spans":[{"text":"Watch ","marks":[]},{"text":"out","marks":[{"kind":"bold"}]}]}"""
        );
        AddBlock(page, all, null, BlockTypes.Divider, """{}""");
        AddBlock(
            page,
            all,
            null,
            BlockTypes.Image,
            """{"url":"https://example.com/cat.png","alt":"a cat","isDecorative":false,"caption":"A fuzzy one"}"""
        );
        AddBlock(
            page,
            all,
            null,
            BlockTypes.Table,
            """{"alignments":["left","right"],"header":[[{"text":"Name","marks":[]}],[{"text":"Qty","marks":[]}]],"rows":[[[{"text":"apples","marks":[]}],[{"text":"3","marks":[]}]]]}"""
        );

        var markdown = MarkdownExporter.Render(
            notebook,
            [page],
            new Dictionary<Guid, IReadOnlyList<Block>> { [page.Id] = all }
        );
        var parsed = MarkdownNotebookParser.Parse("notes.md", markdown);

        Assert.Equal("Notes", parsed.Title);
        var parsedPage = Assert.Single(parsed.Pages);
        Assert.Equal("Page", parsedPage.Title);
        AssertSubtree(null, parsedPage.Blocks);

        void AssertSubtree(Guid? parentId, IReadOnlyList<ParsedMarkdownBlock> actual)
        {
            var expected = all.Where(block => block.ParentBlockId == parentId).ToList();
            Assert.Equal(expected.Count, actual.Count);
            foreach (var (expectedBlock, actualBlock) in expected.Zip(actual))
            {
                Assert.Equal(expectedBlock.Type, actualBlock.Type);
                Assert.Equal(expectedBlock.ContentJson, actualBlock.Content.GetRawText());
                AssertSubtree(expectedBlock.Id, actualBlock.Children);
            }
        }
    }

    [Fact]
    public void PageExportThenImport_RebuildsTheSameBlockTree()
    {
        var page = Page.Create(Guid.CreateVersion7(), null, "Page", "page", "a");

        var all = new List<Block>();
        AddBlock(page, all, null, BlockTypes.Heading, """{"level":2,"spans":[{"text":"Section","marks":[]}]}""");
        AddBlock(
            page,
            all,
            null,
            BlockTypes.Paragraph,
            """{"spans":[{"text":"plain ","marks":[]},{"text":"bold","marks":[{"kind":"bold"}]}]}"""
        );
        AddBlock(
            page,
            all,
            null,
            BlockTypes.Callout,
            """{"variant":"warning","spans":[{"text":"Watch out","marks":[]}]}"""
        );
        AddBlock(page, all, null, BlockTypes.Divider, """{}""");
        var todo = AddBlock(page, all, null, BlockTypes.Todo, """{"checked":false,"spans":[{"text":"parent task","marks":[]}]}""");
        AddBlock(page, all, todo, BlockTypes.Todo, """{"checked":true,"spans":[{"text":"child task","marks":[]}]}""");
        AddBlock(
            page,
            all,
            null,
            BlockTypes.Table,
            """{"alignments":["left"],"header":[[{"text":"Name","marks":[]}]],"rows":[[[{"text":"apples","marks":[]}]]]}"""
        );

        var markdown = MarkdownPageExporter.Render(page, all);
        var parsed = MarkdownPageParser.Parse("page.md", markdown);

        Assert.Equal("Page", parsed.Title);
        AssertSubtree(null, parsed.Blocks);

        void AssertSubtree(Guid? parentId, IReadOnlyList<ParsedMarkdownBlock> actual)
        {
            var expected = all.Where(block => block.ParentBlockId == parentId).ToList();
            Assert.Equal(expected.Count, actual.Count);
            foreach (var (expectedBlock, actualBlock) in expected.Zip(actual))
            {
                Assert.Equal(expectedBlock.Type, actualBlock.Type);
                Assert.Equal(expectedBlock.ContentJson, actualBlock.Content.GetRawText());
                AssertSubtree(expectedBlock.Id, actualBlock.Children);
            }
        }
    }

    private static Block AddBlock(Page page, List<Block> all, Block? parent, string type, string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var normalized = BlockPayloads.ValidateAndNormalize(type, document.RootElement);
        Assert.True(normalized.IsSuccess, normalized.Error?.Message);
        var siblings = all.Where(block => block.ParentBlockId == parent?.Id).ToList();
        var block = Block.Create(
            page.Id,
            parent?.Id,
            type,
            normalized.Value!.CanonicalJson,
            normalized.Value.PlainText,
            BlockSiblingSortKeys.KeyForInsert(siblings, siblings.Count)
        );
        BlockChain.Insert(block, page, parent, siblings.Count > 0 ? siblings[^1] : null, next: null);
        all.Add(block);
        return block;
    }
}
