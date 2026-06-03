using CodeCafe.Mcp.Tools.Notes;
using System.Text.Json;

namespace CodeCafe.Mcp.Tests;

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
}
