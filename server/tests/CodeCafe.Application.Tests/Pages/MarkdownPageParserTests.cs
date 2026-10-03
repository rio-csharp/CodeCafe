using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Application.Pages.ImportPage;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Tests.Pages;

public sealed class MarkdownPageParserTests
{
    private static TPayload Payload<TPayload>(CodeCafe.Application.Common.Markdown.ParsedMarkdownBlock block)
        where TPayload : class
        => BlockPayloads.DeserializeForRead<TPayload>(block.Content.GetRawText());

    [Fact]
    public void FirstH1_BecomesTheTitle_AndLeavesTheContent()
    {
        var page = MarkdownPageParser.Parse("whatever.md", "# My Page\n\nHello");

        Assert.Equal("My Page", page.Title);
        var block = Assert.Single(page.Blocks);
        Assert.Equal(BlockTypes.Paragraph, block.Type);
        Assert.Equal("Hello", block.PlainText);
    }

    [Fact]
    public void LaterH1_StaysContent_ClampedToHeadingLevel1()
    {
        var page = MarkdownPageParser.Parse("x.md", "# Title\n\n# Also content");

        Assert.Equal("Title", page.Title);
        var heading = Payload<HeadingPayload>(Assert.Single(page.Blocks));
        Assert.Equal(1, heading.Level);
        Assert.Equal("Also content", heading.Spans.ToPlainText());
    }

    [Fact]
    public void NoH1_FallsBackToTheFileName()
    {
        var page = MarkdownPageParser.Parse("meeting-notes.md", "just content");

        Assert.Equal("meeting-notes", page.Title);
        Assert.Single(page.Blocks);
    }

    [Fact]
    public void EmptyH1_FallsBackToTheFileName()
    {
        var page = MarkdownPageParser.Parse("fallback.md", "#\n\ncontent");

        Assert.Equal("fallback", page.Title);
    }

    [Fact]
    public void SetextH1_IsNotATitle()
    {
        var page = MarkdownPageParser.Parse("from-file.md", "Title-ish\n===");

        Assert.Equal("from-file", page.Title);
        // The setext line is content: level 1 - offset 1 clamps to heading level 1.
        var heading = Payload<HeadingPayload>(Assert.Single(page.Blocks));
        Assert.Equal(1, heading.Level);
    }

    [Fact]
    public void ContentBeforeTheTitle_IsKept()
    {
        var page = MarkdownPageParser.Parse("x.md", "intro paragraph\n\n# Title");

        Assert.Equal("Title", page.Title);
        Assert.Equal("intro paragraph", Assert.Single(page.Blocks).PlainText);
    }

    [Fact]
    public void ContentHeadings_ParseWithOffset1()
    {
        var page = MarkdownPageParser.Parse("x.md", "# T\n\n## Sub\n\n### SubSub");

        Assert.Equal(2, page.Blocks.Count);
        Assert.Equal(1, Payload<HeadingPayload>(page.Blocks[0]).Level);
        Assert.Equal(2, Payload<HeadingPayload>(page.Blocks[1]).Level);
    }
}
