using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Application.Mcp;
using CodeCafe.Modules.Mcp.Tools.Notes;
using System.Text.Json;

namespace CodeCafe.Modules.Mcp.Tests;

public sealed class MarkdigMcpMarkdownImporterTests
{
    [Fact]
    public void ConvertMarkdownToDocument_HandlesNestedInlineContentWithoutReusingJsonNodes()
    {
        var importer = new MarkdigMcpMarkdownImporter();
        var markdown = """
            ## 1. Why async/await matters

            Use **bold text**, *italic text*, ~~struck text~~, `inline code`, and [a link](https://example.com).

            - A list item with **bold** and [nested link](https://example.com/list)
            - Another item with `code`

            > A quote with **formatting** and [a quote link](https://example.com/quote).

            | Topic | Note |
            | --- | --- |
            | await | `await` does not block the thread |
            | Task | **Tasks** represent async work |
            """;

        var document = importer.ConvertMarkdownToDocument(markdown);

        Assert.Equal("doc", document.GetProperty("type").GetString());
        var raw = document.GetRawText();
        Assert.Contains("\"type\":\"link\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"bold\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"table\"", raw, StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToBlocks_HandlesNestedInlineContentWithoutReusingJsonNodes()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var blocks = importer.ConvertMarkdownToBlocks("A paragraph with **bold** and [a link](https://example.com).");

        Assert.Equal(JsonValueKind.Array, blocks.ValueKind);
        Assert.Contains("\"type\":\"link\"", blocks.GetRawText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n\t")]
    public void ConvertMarkdownToDocument_EmitsParagraphForEmptyMarkdown(string markdown)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(markdown);
        var content = document.GetProperty("content");

        Assert.Single(content.EnumerateArray());
        Assert.Equal("paragraph", content[0].GetProperty("type").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_EmitsParagraphInsideEmptyBlockquote()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(">");
        var quoteContent = document.GetProperty("content")[0].GetProperty("content");

        Assert.Single(quoteContent.EnumerateArray());
        Assert.Equal("paragraph", quoteContent[0].GetProperty("type").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_PreservesLiteralPipeInParagraph()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("**释义** | 陷入，沉溺于（某种情感或生活方式）");

        var paragraphContent = document.GetProperty("content")[0].GetProperty("content");
        Assert.Equal("释义", paragraphContent[0].GetProperty("text").GetString());
        Assert.Equal(" | 陷入，沉溺于（某种情感或生活方式）", string.Concat(
            paragraphContent.EnumerateArray().Skip(1).Select(node => node.GetProperty("text").GetString())));
        Assert.DoesNotContain("PipeTableDelimiterInline", document.GetRawText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("# Page title", 1)]
    [InlineData("##### Heading five", 5)]
    [InlineData("###### Heading six", 6)]
    public void ConvertMarkdownToDocument_PreservesHeadingLevelsForMcpImports(string markdown, int expectedLevel)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(markdown);

        var heading = document.GetProperty("content")[0];
        Assert.Equal("heading", heading.GetProperty("type").GetString());
        Assert.Equal(expectedLevel, heading.GetProperty("attrs").GetProperty("level").GetInt32());
    }

    [Theory]
    [InlineData("A &amp; B", "A & B")]
    [InlineData("HTML is common.\n\n*[HTML]: Hyper Text Markup Language", "HTML is common.")]
    [InlineData("Inline $x^2$ math", "$x^2$")]
    [InlineData("::: warning\ninside **bold**\n:::", "inside bold")]
    [InlineData("^^ Footer **text**", "Footer text")]
    [InlineData("^^^\n![Alt](https://example.com/a.png)\n^^^ Figure caption", "Figure caption")]
    public void ConvertMarkdownToDocument_DoesNotLeakMarkdigTypeNames(
        string markdown,
        string expectedPlainText)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(markdown);
        var raw = document.GetRawText();

        Assert.DoesNotContain("Markdig.", raw, StringComparison.Ordinal);
        Assert.Contains(expectedPlainText, ExtractText(document), StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToDocument_FlattensFootnotesWithoutLeakingInfrastructureNodes()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("Text with a footnote[^1].\n\n[^1]: Footnote **body**");
        var raw = document.GetRawText();
        var text = ExtractText(document);

        Assert.DoesNotContain("Markdig.", raw, StringComparison.Ordinal);
        Assert.Contains("Text with a footnote[1].", text, StringComparison.Ordinal);
        Assert.Contains("Footnote body", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToDocument_PreservesAutolinkSemantics()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("<https://example.com>");

        var textNode = document.GetProperty("content")[0].GetProperty("content")[0];
        Assert.Equal("https://example.com", textNode.GetProperty("text").GetString());
        Assert.Equal("https://example.com", textNode
            .GetProperty("marks")[0]
            .GetProperty("attrs")
            .GetProperty("href")
            .GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_MapsAdvancedEmphasisToSupportedTipTapMarks()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(
            "~~strike~~ ~sub~ ^super^ ++insert++ ==mark== \"\"citation\"\"");
        var raw = document.GetRawText();

        Assert.Contains("\"type\":\"strike\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"subscript\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"superscript\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"underline\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"highlight\"", raw, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"italic\"", raw, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("**before `code` after**", "code")]
    [InlineData("[$x$](https://example.com)", "$x$")]
    public void ConvertMarkdownToDocument_DoesNotCombineCodeWithExclusiveMarks(
        string markdown,
        string expectedText)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(markdown);
        var textNode = FindTextNode(document, expectedText);
        var marks = textNode.GetProperty("marks").EnumerateArray().ToList();

        Assert.Single(marks);
        Assert.Equal("code", marks[0].GetProperty("type").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_DeduplicatesNestedMarks()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("****x****");
        var marks = FindTextNode(document, "x").GetProperty("marks").EnumerateArray().ToList();

        Assert.Single(marks);
        Assert.Equal("bold", marks[0].GetProperty("type").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_EmitsImagesAsTopLevelBlocks()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(
            "before ![Alt](https://example.com/a.png) after");
        var blocks = document.GetProperty("content");

        Assert.Equal(3, blocks.GetArrayLength());
        Assert.Equal("paragraph", blocks[0].GetProperty("type").GetString());
        Assert.Equal("image", blocks[1].GetProperty("type").GetString());
        Assert.Equal("paragraph", blocks[2].GetProperty("type").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_EmitsImagesAsBlocksInsideTaskItems()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(
            "- [x] before ![Alt](https://example.com/a.png) after");
        var itemContent = document
            .GetProperty("content")[0]
            .GetProperty("content")[0]
            .GetProperty("content");

        Assert.Equal(3, itemContent.GetArrayLength());
        Assert.Equal("paragraph", itemContent[0].GetProperty("type").GetString());
        Assert.Equal("image", itemContent[1].GetProperty("type").GetString());
        Assert.Equal("paragraph", itemContent[2].GetProperty("type").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_UsesImageAltTextInsideHeadings()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(
            "# before ![Alt](https://example.com/a.png) after");
        var heading = document.GetProperty("content")[0];

        Assert.Equal("heading", heading.GetProperty("type").GetString());
        Assert.Equal("before Alt after", ExtractText(heading));
        Assert.DoesNotContain("\"type\":\"image\"", heading.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToDocument_PreservesEntitiesInImageAltText()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(
            "![A &amp; B](https://example.com/a.png)");

        var image = document.GetProperty("content")[0];
        Assert.Equal("A & B", image.GetProperty("attrs").GetProperty("alt").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_PreservesCodeBlockTrailingWhitespace()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("```\na  \n\n```");

        var code = document
            .GetProperty("content")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.Equal("a  \n", code);
    }

    [Theory]
    [InlineData("3. Third\n4. Fourth", 3, null)]
    [InlineData("c. Third\nd. Fourth", 3, "a")]
    [InlineData("IV. Fourth\nV. Fifth", 4, "I")]
    public void ConvertMarkdownToDocument_PreservesOrderedListAttributes(
        string markdown,
        int expectedStart,
        string? expectedType)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(markdown);

        var list = document.GetProperty("content")[0];
        Assert.Equal("orderedList", list.GetProperty("type").GetString());
        var attrs = list.GetProperty("attrs");
        Assert.Equal(expectedStart, attrs.GetProperty("start").GetInt32());
        if (expectedType is null)
        {
            Assert.False(attrs.TryGetProperty("type", out _));
        }
        else
        {
            Assert.Equal(expectedType, attrs.GetProperty("type").GetString());
        }
    }

    [Fact]
    public void ConvertMarkdownToDocument_DoesNotTurnMixedBulletListIntoTaskList()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("- ordinary\n- [x] completed");

        Assert.Equal("bulletList", document.GetProperty("content")[0].GetProperty("type").GetString());
    }

    [Theory]
    [InlineData("- ![Alt](https://example.com/a.png)", "bulletList")]
    [InlineData("- [x] ![Alt](https://example.com/a.png)", "taskList")]
    public void ConvertMarkdownToDocument_KeepsImageOnlyListItemsSchemaCompatible(
        string markdown,
        string expectedListType)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(markdown);
        var list = document.GetProperty("content")[0];
        var itemContent = list.GetProperty("content")[0].GetProperty("content");

        Assert.Equal(expectedListType, list.GetProperty("type").GetString());
        Assert.Equal(2, itemContent.GetArrayLength());
        Assert.Equal("paragraph", itemContent[0].GetProperty("type").GetString());
        Assert.Equal("image", itemContent[1].GetProperty("type").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_PreservesGridTableCellSpans()
    {
        var importer = new MarkdigMcpMarkdownImporter();
        var markdown = """
            +---+---+---+
            | AAAAA | B |
            +---+---+ B +
            | D | E | B |
            + D +---+---+
            | D | CCCCC |
            +---+---+---+
            """;

        var document = importer.ConvertMarkdownToDocument(markdown);
        var table = document.GetProperty("content")[0];
        var rows = table.GetProperty("content");

        Assert.Equal(2, rows[0].GetProperty("content")[0].GetProperty("attrs").GetProperty("colspan").GetInt32());
        Assert.Equal(2, rows[0].GetProperty("content")[1].GetProperty("attrs").GetProperty("rowspan").GetInt32());
        Assert.Equal(2, rows[1].GetProperty("content")[0].GetProperty("attrs").GetProperty("rowspan").GetInt32());
        Assert.Equal(2, rows[2].GetProperty("content")[0].GetProperty("attrs").GetProperty("colspan").GetInt32());
    }

    [Fact]
    public void ConvertMarkdownToDocument_PreservesPipeTableColumnAlignment()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("""
            | left | center | right |
            | :--- | :----: | ----: |
            | a | b | c |
            """);
        var rows = document.GetProperty("content")[0].GetProperty("content");

        foreach (var row in rows.EnumerateArray())
        {
            var cells = row.GetProperty("content");
            Assert.Equal("left", cells[0].GetProperty("attrs").GetProperty("align").GetString());
            Assert.Equal("center", cells[1].GetProperty("attrs").GetProperty("align").GetString());
            Assert.Equal("right", cells[2].GetProperty("attrs").GetProperty("align").GetString());
        }
    }

    [Fact]
    public void ConvertMarkdownToDocument_ConvertsYouTubeMediaImageSyntaxToEmbed()
    {
        var importer = new MarkdigMcpMarkdownImporter();
        const string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        var document = importer.ConvertMarkdownToDocument($"![video]({youtubeUrl})");
        var youtube = document.GetProperty("content")[0];

        Assert.Equal("youtube", youtube.GetProperty("type").GetString());
        Assert.Equal(youtubeUrl, youtube.GetProperty("attrs").GetProperty("src").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_CanonicalizesYouTubeUrls()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(
            "![video](https://youtu.be/dQw4w9WgXcQ/?feature=share)");
        var youtube = document.GetProperty("content")[0];

        Assert.Equal("youtube", youtube.GetProperty("type").GetString());
        Assert.Equal(
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            youtube.GetProperty("attrs").GetProperty("src").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_DoesNotAcceptYouTubeUrlsWithExtraPathSegments()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument(
            "![video](https://youtu.be/dQw4w9WgXcQ/extra)");

        Assert.DoesNotContain("\"type\":\"youtube\"", document.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToDocument_ConvertsUnsupportedMediaImageSyntaxToLink()
    {
        var importer = new MarkdigMcpMarkdownImporter();
        const string mediaUrl = "https://cdn.example.com/demo.mp4";

        var document = importer.ConvertMarkdownToDocument($"![video]({mediaUrl})");
        var text = document.GetProperty("content")[0].GetProperty("content")[0];

        Assert.Equal("text", text.GetProperty("type").GetString());
        Assert.Equal("video", text.GetProperty("text").GetString());
        Assert.Equal(mediaUrl, text.GetProperty("marks")[0].GetProperty("attrs").GetProperty("href").GetString());
        Assert.DoesNotContain("\"type\":\"image\"", document.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToDocument_DoesNotTreatLookalikeProviderHostAsMedia()
    {
        var importer = new MarkdigMcpMarkdownImporter();
        const string imageUrl = "https://vimeo.com.evil.example/poster.png";

        var document = importer.ConvertMarkdownToDocument($"![poster]({imageUrl})");
        var image = document.GetProperty("content")[0];

        Assert.Equal("image", image.GetProperty("type").GetString());
        Assert.Equal(imageUrl, image.GetProperty("attrs").GetProperty("src").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_IgnoresLinkReferenceDefinitions()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("""
            Reference [docs][docs-ref].

            [docs-ref]: https://example.com/docs "Docs"
            """);

        var raw = document.GetRawText();
        Assert.DoesNotContain("Markdig.Syntax.LinkReferenceDefinitionGroup", raw, StringComparison.Ordinal);
        Assert.Contains("https://example.com/docs", raw, StringComparison.Ordinal);
        Assert.Single(document.GetProperty("content").EnumerateArray());
    }

    [Fact]
    public void ConvertMarkdownToDocument_ConvertsTaskLists()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("""
            - [x] Done
            - [ ] Todo
            """);

        var taskList = document.GetProperty("content")[0];
        Assert.Equal("taskList", taskList.GetProperty("type").GetString());

        var firstItem = taskList.GetProperty("content")[0];
        var secondItem = taskList.GetProperty("content")[1];
        Assert.Equal("taskItem", firstItem.GetProperty("type").GetString());
        Assert.True(firstItem.GetProperty("attrs").GetProperty("checked").GetBoolean());
        Assert.False(secondItem.GetProperty("attrs").GetProperty("checked").GetBoolean());

        var firstText = firstItem
            .GetProperty("content")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.Equal("Done", firstText);
    }

    [Fact]
    public void ConvertMarkdownToDocument_DoesNotEmitEmptyTextNodesForEmptyCodeBlocks()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("""
            ```csharp
            ```
            """);

        Assert.DoesNotContain("\"text\":\"\"", document.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToDocument_ConvertsHardLineBreaks()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("Line one  \nLine two");

        Assert.Contains("\"type\":\"hardBreak\"", document.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ConvertMarkdownToDocument_PreservesCurlyDoubleQuotes()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("Quote: \u201Cvalue\u201D");

        var text = document
            .GetProperty("content")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        var raw = document.GetRawText();
        Assert.Equal("Quote: \u201Cvalue\u201D", text);
        Assert.Contains("\u201C", raw, StringComparison.Ordinal);
        Assert.Contains("\u201D", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u201C", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u201D", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("//evil.example/phish")]
    public void ConvertMarkdownToDocument_RemovesLinkMarkForUnsafeUrls(string url)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument($"[click me]({url})");

        Assert.DoesNotContain("\"type\":\"link\"", document.GetRawText(), StringComparison.Ordinal);
        var paragraph = document.GetProperty("content")[0];
        Assert.Equal("click me", paragraph.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Theory]
    [InlineData("https://example.com/page")]
    [InlineData("mailto:user@example.com")]
    [InlineData("tel:+1234567890")]
    [InlineData("/docs/getting-started")]
    public void ConvertMarkdownToDocument_KeepsLinkMarkForAllowedUrls(string url)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument($"[click me]({url})");

        var textNode = document.GetProperty("content")[0].GetProperty("content")[0];
        var linkMark = textNode.GetProperty("marks")[0];
        Assert.Equal("link", linkMark.GetProperty("type").GetString());
        Assert.Equal(url, linkMark.GetProperty("attrs").GetProperty("href").GetString());
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    [InlineData("//evil.example/tracker.png")]
    public void ConvertMarkdownToDocument_RemovesImageSrcForUnsafeUrls(string url)
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument($"![alt text]({url})");

        var image = document.GetProperty("content")[0];
        Assert.Equal("image", image.GetProperty("type").GetString());
        Assert.False(image.GetProperty("attrs").TryGetProperty("src", out _));
        Assert.Equal("alt text", image.GetProperty("attrs").GetProperty("alt").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_KeepsImageSrcForAllowedUrls()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("![alt text](https://cdn.example.com/a.png)");

        var image = document.GetProperty("content")[0];
        Assert.Equal("image", image.GetProperty("type").GetString());
        Assert.Equal("https://cdn.example.com/a.png", image.GetProperty("attrs").GetProperty("src").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_RejectsPathologicallyNestedInput_WithoutExhaustingTheStack()
    {
        // The block converter recurses over the parsed tree with no depth guard of its own. It is safe
        // because Markdig refuses to parse input nested past its own limit, so the converter never
        // sees such a tree. This test pins that dependency: if a Markdig upgrade dropped the limit,
        // the recursion would become a stack-overflow risk and this test would stop throwing.
        var importer = new MarkdigMcpMarkdownImporter();
        var deeplyNested = string.Concat(Enumerable.Repeat("> ", 5000)) + "boom";

        var exception = Record.Exception(() => importer.ConvertMarkdownToDocument(deeplyNested));

        Assert.NotNull(exception);
        Assert.IsType<ArgumentException>(exception);
        Assert.Contains("deeply nested", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConvertMarkdownToDocument_AcceptsModeratelyNestedInput()
    {
        // Guards the other side: ordinary nesting must stay well inside Markdig's limit.
        var importer = new MarkdigMcpMarkdownImporter();
        var nested = string.Concat(Enumerable.Repeat("> ", 20)) + "still fine";

        var document = importer.ConvertMarkdownToDocument(nested);

        Assert.Equal("doc", document.GetProperty("type").GetString());
    }

    private static string ExtractText(JsonElement element)
    {
        var parts = new List<string>();
        AddText(element, parts);
        return string.Concat(parts);
    }

    private static JsonElement FindTextNode(JsonElement element, string text)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out var type)
                && type.GetString() == "text"
                && element.TryGetProperty("text", out var textProperty)
                && textProperty.GetString() == text)
            {
                return element;
            }

            if (element.TryGetProperty("content", out var content))
            {
                var match = FindTextNode(content, text);
                if (match.ValueKind != JsonValueKind.Undefined)
                {
                    return match;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                var match = FindTextNode(child, text);
                if (match.ValueKind != JsonValueKind.Undefined)
                {
                    return match;
                }
            }
        }

        return default;
    }

    private static void AddText(JsonElement element, List<string> parts)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out var type)
                && type.GetString() == "text"
                && element.TryGetProperty("text", out var text))
            {
                parts.Add(text.GetString() ?? string.Empty);
            }

            if (element.TryGetProperty("content", out var content))
            {
                AddText(content, parts);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                AddText(child, parts);
            }
        }
    }
}
