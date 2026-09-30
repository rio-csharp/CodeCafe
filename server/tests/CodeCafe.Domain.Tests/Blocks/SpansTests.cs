using CodeCafe.Domain.Blocks;

namespace CodeCafe.Domain.Tests.Blocks;

public sealed class SpansTests
{
    private static readonly LinkMark Link = new("https://example.com");

    [Fact]
    public void Create_MergesAdjacentSpansWithIdenticalMarkSets()
    {
        var spans = Spans.Create(
        [
            new Span("a", [new BoldMark()]),
            new Span("b", [new BoldMark()])
        ]);

        var span = Assert.Single(spans.Items);
        Assert.Equal("ab", span.Text);
        Assert.Equal([new BoldMark()], span.Marks);
    }

    [Fact]
    public void Create_DifferentMarkSets_DoNotMerge()
    {
        var spans = Spans.Create(
        [
            new Span("a", [new BoldMark()]),
            new Span("b", [])
        ]);

        Assert.Equal(2, spans.Items.Count);
    }

    [Fact]
    public void Create_DropsEmptyTextSpans()
    {
        var spans = Spans.Create(
        [
            new Span("", [new BoldMark()]),
            new Span("x", [])
        ]);

        var span = Assert.Single(spans.Items);
        Assert.Equal("x", span.Text);
        Assert.Empty(span.Marks);
    }

    [Fact]
    public void Create_DroppingEmptiesMakesNeighboursAdjacentAndMergeable()
    {
        var spans = Spans.Create(
        [
            new Span("a", [new BoldMark()]),
            new Span("", []),
            new Span("b", [new BoldMark()])
        ]);

        var span = Assert.Single(spans.Items);
        Assert.Equal("ab", span.Text);
    }

    [Fact]
    public void Create_AllEmpty_IsTheSingleLegalEmptyForm()
    {
        Assert.Same(Spans.Empty, Spans.Create([new Span("", [])]));
        Assert.Empty(Spans.Create([]).Items);
    }

    [Fact]
    public void Create_OrdersMarksCanonically()
    {
        var spans = Spans.Create(
        [
            new Span("x", [new ColorMark(PaletteColor.Danger), new BoldMark(), Link])
        ]);

        Assert.Equal<Mark>([Link, new BoldMark(), new ColorMark(PaletteColor.Danger)], spans.Items[0].Marks);
    }

    [Fact]
    public void Create_DeduplicatesIdenticalMarks()
    {
        var spans = Spans.Create([new Span("x", [new BoldMark(), new BoldMark()])]);

        Assert.Equal<Mark>([new BoldMark()], spans.Items[0].Marks);
    }

    [Fact]
    public void Create_NeverTrimsWhitespace()
    {
        var spans = Spans.Create([new Span("  x  ", [])]);

        Assert.Equal("  x  ", spans.Items[0].Text);
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\r\nb")]
    public void Create_LineBreakInText_Throws(string text) =>
        Assert.Throws<ArgumentException>(() => Spans.Create([new Span(text, [])]));

    [Fact]
    public void Create_SupAndSub_Throws() =>
        Assert.Throws<ArgumentException>(
            () => Spans.Create([new Span("x", [new SupMark(), new SubMark()])]));

    [Fact]
    public void Create_TwoLinks_Throws() =>
        Assert.Throws<ArgumentException>(
            () => Spans.Create([new Span("x", [Link, new LinkMark("https://other.example.com")])]));

    [Fact]
    public void Create_TwoColors_Throws() =>
        Assert.Throws<ArgumentException>(
            () => Spans.Create([new Span("x", [new ColorMark(PaletteColor.Danger), new ColorMark(PaletteColor.Info)])]));

    [Fact]
    public void Create_TwoHighlights_Throws() =>
        Assert.Throws<ArgumentException>(
            () => Spans.Create([new Span("x", [new HighlightMark(PaletteColor.Info), new HighlightMark(PaletteColor.Muted)])]));

    [Fact]
    public void Create_WhitespaceOnlyLinkRun_Throws() =>
        Assert.Throws<ArgumentException>(
            () => Spans.Create([new Span(" ", [Link]), new Span("\t", [Link])]));

    [Fact]
    public void Create_LinkRunSplitAcrossStyles_ValidatesAsOneRun()
    {
        // The same link split by a style boundary is legal: the run's aggregate text is visible.
        var spans = Spans.Create(
        [
            new Span("he", [Link, new BoldMark()]),
            new Span("llo", [Link])
        ]);

        Assert.Equal(2, spans.Items.Count);
        Assert.Equal("hello", spans.ToPlainText());
    }

    [Fact]
    public void Create_DifferentLinks_BreakTheRun()
    {
        var other = new LinkMark("https://other.example.com");

        Assert.Throws<ArgumentException>(
            () => Spans.Create([new Span("x", [Link]), new Span(" ", [other])]));
    }

    [Fact]
    public void ToPlainText_ConcatenatesSpanTexts()
    {
        var spans = Spans.Create(
        [
            new Span("a ", [new BoldMark()]),
            new Span("b", [])
        ]);

        Assert.Equal("a b", spans.ToPlainText());
        Assert.Equal(string.Empty, Spans.Empty.ToPlainText());
    }

    [Fact]
    public void Equals_ComparesByValue()
    {
        var left = Spans.Create([new Span("x", [new BoldMark()])]);
        var right = Spans.Create([new Span("x", [new BoldMark()])]);

        Assert.Equal(left, right);
        Assert.NotEqual(left, Spans.Create([new Span("y", [new BoldMark()])]));
    }
}
