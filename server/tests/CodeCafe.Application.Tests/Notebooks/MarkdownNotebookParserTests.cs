using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Application.Notebooks.ImportNotebook;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class MarkdownNotebookParserTests
{
    [Fact]
    public void Parse_SplitsPagesAtLevelTwoHeadings()
    {
        var parsed = MarkdownNotebookParser.Parse(
            "notes.md",
            "# My Notebook\n\n## First\n\nalpha\n\n## Second\n\nbravo"
        );

        Assert.Equal("My Notebook", parsed.Title);
        Assert.Equal(2, parsed.Pages.Count);
        Assert.Equal("First", parsed.Pages[0].Title);
        Assert.Equal("alpha", Assert.Single(parsed.Pages[0].Blocks).PlainText);
        Assert.Equal("Second", parsed.Pages[1].Title);
        Assert.Equal("bravo", Assert.Single(parsed.Pages[1].Blocks).PlainText);
    }

    [Fact]
    public void Parse_WithoutLevelOneHeading_FallsBackToFileName()
    {
        var parsed = MarkdownNotebookParser.Parse("my-notes.md", "## Page\n\ncontent");

        Assert.Equal("my-notes", parsed.Title);
    }

    [Fact]
    public void Parse_PageTitleStripsInlineMarks()
    {
        var parsed = MarkdownNotebookParser.Parse("notes.md", "## **Bold** Page\n\nx");

        Assert.Equal("Bold Page", parsed.Pages[0].Title);
    }

    [Fact]
    public void Parse_DropsContentBeforeTheFirstPageHeading()
    {
        var parsed = MarkdownNotebookParser.Parse("notes.md", "# Notebook\n\norphan text\n\n## Page\n\nkept");

        Assert.Single(parsed.Pages);
        Assert.Equal("kept", Assert.Single(parsed.Pages[0].Blocks).PlainText);
    }

    [Fact]
    public void Parse_ContentHeadingsShiftByTwoLevels()
    {
        var parsed = MarkdownNotebookParser.Parse("notes.md", "## Page\n\n### Section");

        var block = Assert.Single(parsed.Pages[0].Blocks);
        Assert.Equal(BlockTypes.Heading, block.Type);
        var heading = BlockPayloads.DeserializeForRead<HeadingPayload>(block.Content.GetRawText());
        Assert.Equal(1, heading.Level);
    }

    [Fact]
    public void Parse_SetextHeading_DoesNotSplitPages()
    {
        // "Page\n---" is a setext level-2 heading, not a "##" page marker; it stays content.
        var parsed = MarkdownNotebookParser.Parse("notes.md", "## Real\n\nTitle\n---");

        var page = Assert.Single(parsed.Pages);
        Assert.Equal("Real", page.Title);
        Assert.Equal(BlockTypes.Heading, Assert.Single(page.Blocks).Type);
    }
}
