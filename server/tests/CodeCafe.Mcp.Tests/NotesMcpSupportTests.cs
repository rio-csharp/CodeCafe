using System.Text.Json;

namespace CodeCafe.Host.Mcp.Tests;

public sealed class NotesMcpSupportTests
{
    private static readonly JsonElement SampleDoc = JsonSerializer.SerializeToElement(
        new
        {
            type = "doc",
            content = new[]
            {
                new
                {
                    type = "paragraph",
                    content = new[] { new { type = "text", text = "First paragraph." } },
                },
                new
                {
                    type = "paragraph",
                    content = new[] { new { type = "text", text = "Second paragraph." } },
                },
            },
        }
    );

    [Fact]
    public void ReplaceBlockAtIndex_ReplacesSpecifiedBlock()
    {
        var heading = JsonSerializer.SerializeToElement(
            new
            {
                type = "heading",
                attrs = new { level = 1 },
                content = new[] { new { type = "text", text = "Heading" } },
            }
        );

        var result = NotesMcpSupport.ReplaceBlockAtIndex(SampleDoc, 0, heading);

        var content = result.GetProperty("content");
        Assert.Equal(2, content.GetArrayLength());
        Assert.Equal("heading", content[0].GetProperty("type").GetString());
        Assert.Equal("paragraph", content[1].GetProperty("type").GetString());
    }

    [Fact]
    public void ReplaceBlockAtIndex_ThrowsWhenIndexOutOfRange()
    {
        var block = JsonSerializer.SerializeToElement(new { type = "paragraph" });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NotesMcpSupport.ReplaceBlockAtIndex(SampleDoc, 5, block)
        );
    }

    [Fact]
    public void InsertBlocksAtIndex_InsertsAtBeginning()
    {
        var blocks = JsonSerializer.SerializeToElement(
            new[]
            {
                new
                {
                    type = "heading",
                    attrs = new { level = 2 },
                    content = new[] { new { type = "text", text = "Inserted" } },
                },
            }
        );

        var result = NotesMcpSupport.InsertBlocksAtIndex(SampleDoc, 0, blocks);

        var content = result.GetProperty("content");
        Assert.Equal(3, content.GetArrayLength());
        Assert.Equal("heading", content[0].GetProperty("type").GetString());
        Assert.Equal("paragraph", content[1].GetProperty("type").GetString());
    }

    [Fact]
    public void InsertBlocksAtIndex_InsertsAtEnd()
    {
        var blocks = JsonSerializer.SerializeToElement(
            new[]
            {
                new
                {
                    type = "paragraph",
                    content = new[] { new { type = "text", text = "Last." } },
                },
            }
        );

        var result = NotesMcpSupport.InsertBlocksAtIndex(SampleDoc, 2, blocks);

        var content = result.GetProperty("content");
        Assert.Equal(3, content.GetArrayLength());
        Assert.Equal("paragraph", content[2].GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void InsertBlocksAtIndex_ThrowsWhenIndexOutOfRange(int index)
    {
        var blocks = JsonSerializer.SerializeToElement(
            new[]
            {
                new
                {
                    type = "paragraph",
                    content = new[] { new { type = "text", text = "Inserted." } },
                },
            }
        );

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NotesMcpSupport.InsertBlocksAtIndex(SampleDoc, index, blocks)
        );
    }

    [Fact]
    public void DeleteBlockAtIndex_RemovesSpecifiedBlock()
    {
        var result = NotesMcpSupport.DeleteBlockAtIndex(SampleDoc, 0);

        var content = result.GetProperty("content");
        Assert.Single(content.EnumerateArray());
        Assert.Equal(
            "Second paragraph.",
            content[0].GetProperty("content")[0].GetProperty("text").GetString()
        );
    }

    [Fact]
    public void DeleteBlockAtIndex_ThrowsWhenIndexOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NotesMcpSupport.DeleteBlockAtIndex(SampleDoc, 5)
        );
    }

    [Fact]
    public void ReplaceTextInDocument_ReplacesFirstOccurrence()
    {
        var result = NotesMcpSupport.ReplaceTextInDocument(
            SampleDoc,
            "paragraph",
            "section",
            replaceAll: false
        );

        var content = result.GetProperty("content");
        Assert.Equal(
            "First section.",
            content[0].GetProperty("content")[0].GetProperty("text").GetString()
        );
        Assert.Equal(
            "Second paragraph.",
            content[1].GetProperty("content")[0].GetProperty("text").GetString()
        );
    }

    [Fact]
    public void ReplaceTextInDocument_ReplacesOnlyFirstOccurrenceInsideSameTextNode()
    {
        var doc = JsonSerializer.SerializeToElement(
            new
            {
                type = "doc",
                content = new[]
                {
                    new
                    {
                        type = "paragraph",
                        content = new[] { new { type = "text", text = "alpha alpha alpha" } },
                    },
                },
            }
        );

        var result = NotesMcpSupport.ReplaceTextInDocument(doc, "alpha", "beta", replaceAll: false);

        var text = result
            .GetProperty("content")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.Equal("beta alpha alpha", text);
    }

    [Fact]
    public void ReplaceTextInDocument_ReplacesAllOccurrences()
    {
        var result = NotesMcpSupport.ReplaceTextInDocument(
            SampleDoc,
            "paragraph",
            "section",
            replaceAll: true
        );

        var content = result.GetProperty("content");
        Assert.Equal(
            "First section.",
            content[0].GetProperty("content")[0].GetProperty("text").GetString()
        );
        Assert.Equal(
            "Second section.",
            content[1].GetProperty("content")[0].GetProperty("text").GetString()
        );
    }

    [Fact]
    public void ReplaceTextInDocument_ThrowsWhenTextNotFound()
    {
        Assert.Throws<ArgumentException>(() =>
            NotesMcpSupport.ReplaceTextInDocument(
                SampleDoc,
                "missing",
                "section",
                replaceAll: false
            )
        );
    }
}
