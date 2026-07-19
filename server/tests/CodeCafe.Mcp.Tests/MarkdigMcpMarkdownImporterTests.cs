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

    [Fact]
    public void ConvertMarkdownToDocument_PreservesHeadingLevelsForMcpImports()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("# Page title");

        var heading = document.GetProperty("content")[0];
        Assert.Equal("heading", heading.GetProperty("type").GetString());
        Assert.Equal(1, heading.GetProperty("attrs").GetProperty("level").GetInt32());
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

        var image = document.GetProperty("content")[0].GetProperty("content")[0];
        Assert.Equal("image", image.GetProperty("type").GetString());
        Assert.False(image.GetProperty("attrs").TryGetProperty("src", out _));
        Assert.Equal("alt text", image.GetProperty("attrs").GetProperty("alt").GetString());
    }

    [Fact]
    public void ConvertMarkdownToDocument_KeepsImageSrcForAllowedUrls()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("![alt text](https://cdn.example.com/a.png)");

        var image = document.GetProperty("content")[0].GetProperty("content")[0];
        Assert.Equal("image", image.GetProperty("type").GetString());
        Assert.Equal("https://cdn.example.com/a.png", image.GetProperty("attrs").GetProperty("src").GetString());
    }
}
