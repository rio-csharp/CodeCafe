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

    [Fact]
    public void ConvertMarkdownToDocument_PreservesH1HeadingsForMcpImports()
    {
        var importer = new MarkdigMcpMarkdownImporter();

        var document = importer.ConvertMarkdownToDocument("# Page title");

        var heading = document.GetProperty("content")[0];
        Assert.Equal("heading", heading.GetProperty("type").GetString());
        Assert.Equal(1, heading.GetProperty("attrs").GetProperty("level").GetInt32());
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
}
