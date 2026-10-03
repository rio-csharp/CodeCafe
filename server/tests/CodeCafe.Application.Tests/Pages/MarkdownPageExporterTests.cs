using System.Text.Json;

using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Pages;

// Exercises the shared block renderer through the page exporter: block formats, escapes, mark
// rendering and the Level + 1 heading offset. Notebook-level specifics (page ordering, +2
// offset) live in MarkdownExporterTests.
public sealed class MarkdownPageExporterTests
{
    private static Page NewPage() => Page.Create(Guid.CreateVersion7(), null, "My Page", "my-page", "a");

    private static string Render(params (string Type, string PayloadJson)[] specs)
    {
        var page = NewPage();
        var blocks = new List<Block>();
        foreach (var (type, payloadJson) in specs)
        {
            AddBlock(page, blocks, null, type, payloadJson);
        }

        return MarkdownPageExporter.Render(page, blocks).ReplaceLineEndings("\n");
    }

    [Fact]
    public void Title_RendersAsH1_WithTrailingNewline()
    {
        var markdown = Render();

        Assert.Equal("# My Page\n", markdown);
    }

    [Fact]
    public void Paragraph_RendersPlainText()
    {
        var markdown = Render((BlockTypes.Paragraph, """{"spans":[{"text":"hello","marks":[]}]}"""));

        Assert.Contains("\nhello\n", markdown);
    }

    [Fact]
    public void Paragraph_LeadingHash_IsEscaped()
    {
        var markdown = Render((BlockTypes.Paragraph, """{"spans":[{"text":"# not a heading","marks":[]}]}"""));

        Assert.Contains("\n\\# not a heading\n", markdown);
    }

    [Fact]
    public void Paragraph_MarkerCharacters_AreEscaped()
    {
        var markdown = Render((BlockTypes.Paragraph, """{"spans":[{"text":"a *b* _c_ `d` [e] \\f","marks":[]}]}"""));

        Assert.Contains(@"a \*b\* \_c\_ \`d\` \[e\] \\f", markdown);
    }

    [Fact]
    public void Paragraph_Marks_RenderAsMarkdown()
    {
        var markdown = Render((
            BlockTypes.Paragraph,
            """{"spans":[{"text":"b","marks":[{"kind":"bold"}]},{"text":" ","marks":[]},{"text":"i","marks":[{"kind":"italic"}]},{"text":" ","marks":[]},{"text":"s","marks":[{"kind":"strike"}]},{"text":" ","marks":[]},{"text":"c","marks":[{"kind":"code"}]},{"text":" ","marks":[]},{"text":"l","marks":[{"kind":"link","href":"https://example.com"}]}]}"""
        ));

        Assert.Contains("**b** *i* ~~s~~ `c` [l](https://example.com)", markdown);
    }

    [Fact]
    public void Paragraph_HtmlOnlyMarks_RenderAsTags()
    {
        var markdown = Render((
            BlockTypes.Paragraph,
            """{"spans":[{"text":"u","marks":[{"kind":"underline"}]},{"text":" ","marks":[]},{"text":"k","marks":[{"kind":"kbd"}]},{"text":" ","marks":[]},{"text":"up","marks":[{"kind":"sup"}]},{"text":" ","marks":[]},{"text":"dn","marks":[{"kind":"sub"}]},{"text":" ","marks":[]},{"text":"a","marks":[{"kind":"abbr","title":"Abbreviation"}]}]}"""
        ));

        Assert.Contains("<u>u</u> <kbd>k</kbd> <sup>up</sup> <sub>dn</sub> <abbr title=\"Abbreviation\">a</abbr>", markdown);
    }

    [Fact]
    public void Paragraph_AbbrTitle_EscapesQuotes()
    {
        var markdown = Render((
            BlockTypes.Paragraph,
            """{"spans":[{"text":"a","marks":[{"kind":"abbr","title":"say \"hi\""}]}]}"""
        ));

        Assert.Contains("<abbr title=\"say &quot;hi&quot;\">a</abbr>", markdown);
    }

    [Fact]
    public void Paragraph_ColorMarks_RenderBareText()
    {
        var markdown = Render((
            BlockTypes.Paragraph,
            """{"spans":[{"text":"x","marks":[{"kind":"color","name":"danger"}]},{"text":" ","marks":[]},{"text":"y","marks":[{"kind":"highlight","name":"warning"}]}]}"""
        ));

        Assert.Contains("\nx y\n", markdown);
    }

    [Fact]
    public void Heading_RendersLevelPlusOneHash()
    {
        var markdown = Render((BlockTypes.Heading, """{"level":2,"spans":[{"text":"Sub","marks":[]}]}"""));

        Assert.Contains("\n### Sub\n", markdown);
    }

    [Fact]
    public void Heading_CapsAtSixHashes()
    {
        var markdown = Render((BlockTypes.Heading, """{"level":6,"spans":[{"text":"Deep","marks":[]}]}"""));

        Assert.Contains("\n###### Deep\n", markdown);
    }

    [Theory]
    [InlineData(false, "- [ ] ")]
    [InlineData(true, "- [x] ")]
    public void Todo_RendersCheckbox(bool isChecked, string expectedPrefix)
    {
        var checkedJson = isChecked ? "true" : "false";
        var markdown = Render((BlockTypes.Todo, $$"""{"checked":{{checkedJson}},"spans":[{"text":"task","marks":[]}]}"""));

        Assert.Contains($"\n{expectedPrefix}task\n", markdown);
    }

    [Fact]
    public void Code_RendersFenceWithLanguage()
    {
        var markdown = Render((BlockTypes.Code, """{"code":"var x = 1;","language":"csharp"}"""));

        Assert.Contains("\n```csharp\nvar x = 1;\n```\n", markdown);
    }

    [Fact]
    public void Quote_RendersWithQuotePrefix()
    {
        var markdown = Render((BlockTypes.Quote, """{"spans":[{"text":"wise","marks":[]}]}"""));

        Assert.Contains("\n> wise\n", markdown);
    }

    [Fact]
    public void Callout_RendersObsidianStyle_WithUppercaseVariant()
    {
        var markdown = Render((BlockTypes.Callout, """{"variant":"warning","spans":[{"text":"careful","marks":[]}]}"""));

        Assert.Contains("\n> [!WARNING]\n> careful\n", markdown);
    }

    [Fact]
    public void Divider_RendersThematicBreak()
    {
        var markdown = Render((BlockTypes.Divider, """{}"""));

        Assert.Contains("\n---\n", markdown);
    }

    [Fact]
    public void Image_WithCaption_RendersImagePlusItalicLine()
    {
        var markdown = Render((
            BlockTypes.Image,
            """{"url":"https://example.com/cat.png","alt":"a cat","isDecorative":false,"caption":"A fuzzy one"}"""
        ));

        Assert.Contains("\n![a cat](https://example.com/cat.png)\n_A fuzzy one_\n", markdown);
    }

    [Fact]
    public void Image_Decorative_RendersEmptyAlt()
    {
        var markdown = Render((
            BlockTypes.Image,
            """{"url":"https://example.com/cat.png","alt":null,"isDecorative":true,"caption":null}"""
        ));

        Assert.Contains("\n![](https://example.com/cat.png)\n", markdown);
    }

    [Fact]
    public void Image_UrlWithParen_PercentEncodesIt()
    {
        var markdown = Render((
            BlockTypes.Image,
            """{"url":"https://example.com/a(1).png","alt":null,"isDecorative":true,"caption":null}"""
        ));

        Assert.Contains("![](https://example.com/a(1%29.png)", markdown);
    }

    [Fact]
    public void Audio_RendersAsLabeledLink()
    {
        var markdown = Render((BlockTypes.Audio, """{"url":"https://example.com/a.mp3","mimeType":"audio/mpeg"}"""));

        Assert.Contains("\n[audio](https://example.com/a.mp3)\n", markdown);
    }

    [Fact]
    public void Table_RendersHeaderAlignmentsAndRows()
    {
        var markdown = Render((
            BlockTypes.Table,
            """{"alignments":["left","right"],"header":[[{"text":"Name","marks":[]}],[{"text":"Qty","marks":[]}]],"rows":[[[{"text":"apples","marks":[]}],[{"text":"3","marks":[]}]]]}"""
        ));

        Assert.Contains("| Name | Qty |\n| :--- | ---: |\n| apples | 3 |\n", markdown);
    }

    [Fact]
    public void Table_Headerless_RendersEmptyHeaderRow()
    {
        var markdown = Render((
            BlockTypes.Table,
            """{"alignments":["none"],"header":null,"rows":[[[{"text":"x","marks":[]}]]]}"""
        ));

        Assert.Contains("|  |\n| --- |\n| x |\n", markdown);
    }

    [Fact]
    public void Table_PipeInCell_IsEscaped()
    {
        var markdown = Render((
            BlockTypes.Table,
            """{"alignments":["none"],"header":[[{"text":"a|b","marks":[]}]],"rows":[]}"""
        ));

        Assert.Contains(@"| a\|b |", markdown);
    }

    [Fact]
    public void UnknownType_RendersHtmlComment()
    {
        var page = NewPage();
        var blocks = new List<Block>();
        // Bypass payload validation: the point is exactly that the type is unknown.
        var block = Block.Create(page.Id, null, "hologram", "{}", string.Empty, "a");
        BlockChain.Insert(block, page, null, null, null);
        blocks.Add(block);

        var markdown = MarkdownPageExporter.Render(page, blocks).ReplaceLineEndings("\n");

        Assert.Contains("\n<!-- unsupported block: hologram -->\n", markdown);
    }

    [Fact]
    public void NestedBlocks_RenderIndented()
    {
        var page = NewPage();
        var blocks = new List<Block>();
        var parent = AddBlock(page, blocks, null, BlockTypes.Paragraph, """{"spans":[{"text":"parent","marks":[]}]}""");
        AddBlock(page, blocks, parent, BlockTypes.Paragraph, """{"spans":[{"text":"child","marks":[]}]}""");

        var markdown = MarkdownPageExporter.Render(page, blocks).ReplaceLineEndings("\n");

        Assert.Contains("\nparent\n\n  child\n", markdown);
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
