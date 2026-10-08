using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Application.Common.Markdown;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Tests.Common;

public sealed class MarkdownDocumentParserTests
{
    private static IReadOnlyList<ParsedMarkdownBlock> Parse(string markdown, int headingLevelOffset = 0)
        => MarkdownDocumentParser.ParseBlocks(MarkdownDocumentParser.ParseDocument(markdown), headingLevelOffset);

    private static TPayload Payload<TPayload>(ParsedMarkdownBlock block)
        where TPayload : class
        => BlockPayloads.DeserializeForRead<TPayload>(block.Content.GetRawText());

    #region Paragraphs and inline marks

    [Fact]
    public void Paragraph_InlineMarks_MapToTypedMarks()
    {
        var blocks = Parse("plain **bold** *italic* ~~gone~~ `code`");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        Assert.Equal("plain bold italic gone code", spans.ToPlainText());
        AssertSpan(spans, "bold", typeof(BoldMark));
        AssertSpan(spans, "italic", typeof(ItalicMark));
        AssertSpan(spans, "gone", typeof(StrikeMark));
        AssertSpan(spans, "code", typeof(CodeMark));
    }

    [Fact]
    public void Paragraph_ExtendedEmphasis_MapToTypedMarks()
    {
        var blocks = Parse("~sub~ ^sup^ ==highlight== ++inserted++");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        Assert.Equal("sub sup highlight inserted", spans.ToPlainText());
        AssertSpan(spans, "sub", typeof(SubMark));
        AssertSpan(spans, "sup", typeof(SupMark));
        AssertSpan(spans, "inserted", typeof(UnderlineMark));
        var highlight = spans.Items.Single(span => span.Text == "highlight");
        var mark = Assert.IsType<HighlightMark>(Assert.Single(highlight.Marks));
        // Markdown carries no color information; warning (yellow) is the default.
        Assert.Equal(PaletteColor.Warning, mark.Name);
    }

    [Fact]
    public void Paragraph_UnderscoreEmphasis_MapsSameAsAsterisk()
    {
        var blocks = Parse("__bold__ _italic_");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        AssertSpan(spans, "bold", typeof(BoldMark));
        AssertSpan(spans, "italic", typeof(ItalicMark));
    }

    [Fact]
    public void Paragraph_NestedEmphasis_CombinesMarks()
    {
        var blocks = Parse("***both***");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        var span = Assert.Single(spans.Items);
        Assert.Equal("both", span.Text);
        Assert.Equal(2, span.Marks.Count);
        Assert.Contains(span.Marks, mark => mark is BoldMark);
        Assert.Contains(span.Marks, mark => mark is ItalicMark);
    }

    [Fact]
    public void Paragraph_AbsoluteLink_BecomesLinkMark()
    {
        var blocks = Parse("see [the docs](https://example.com/docs)");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        AssertSpan(spans, "the docs", typeof(LinkMark));
        var link = (LinkMark)spans.Items.Single(span => span.Text == "the docs").Marks[0];
        Assert.Equal("https://example.com/docs", link.Href);
    }

    [Fact]
    public void Paragraph_MailtoLink_BecomesLinkMark()
    {
        var blocks = Parse("[mail me](mailto:a@example.com)");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        AssertSpan(spans, "mail me", typeof(LinkMark));
    }

    [Fact]
    public void Paragraph_RelativeLink_DegradesToPlainText()
    {
        var blocks = Parse("[local](./other.md)");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        Assert.Equal("local", spans.ToPlainText());
        Assert.DoesNotContain(spans.Items, span => span.Marks.Any(mark => mark is LinkMark));
    }

    [Fact]
    public void Paragraph_WhitespaceOnlyLinkText_DropsLinkMarkKeepsText()
    {
        var blocks = Parse("[ ](https://example.com)");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        Assert.Equal(" ", spans.ToPlainText());
        Assert.DoesNotContain(spans.Items, span => span.Marks.Any(mark => mark is LinkMark));
    }

    [Fact]
    public void Paragraph_LinkAroundEmphasis_KeepsBothMarks()
    {
        var blocks = Parse("[**bold**](https://example.com)");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        var span = Assert.Single(spans.Items);
        Assert.Equal("bold", span.Text);
        Assert.Contains(span.Marks, mark => mark is BoldMark);
        Assert.Contains(span.Marks, mark => mark is LinkMark);
    }

    [Fact]
    public void Paragraph_ReferenceLink_ResolvesAndDefinitionIsNotEmitted()
    {
        var blocks = Parse("[text][ref]\n\n[ref]: https://example.com");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        AssertSpan(spans, "text", typeof(LinkMark));
    }

    [Fact]
    public void Paragraph_EscapedMarkers_StayLiteral()
    {
        var blocks = Parse(@"\*not italic\*");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        Assert.Equal("*not italic*", spans.ToPlainText());
        Assert.All(spans.Items, span => Assert.Empty(span.Marks));
    }

    [Fact]
    public void Paragraph_EscapedHash_StaysParagraph()
    {
        var blocks = Parse(@"\# not a heading");

        var block = Assert.Single(blocks);
        Assert.Equal(BlockTypes.Paragraph, block.Type);
        Assert.Equal("# not a heading", block.PlainText);
    }

    [Fact]
    public void Paragraph_HtmlEntities_AreDecoded()
    {
        var blocks = Parse("Fish &amp; Chips");

        Assert.Equal("Fish & Chips", Payload<ParagraphPayload>(Assert.Single(blocks)).Spans.ToPlainText());
    }

    [Fact]
    public void Paragraph_InlineHtml_TagsAreStrippedTextSurvives()
    {
        var blocks = Parse("a <b>bold</b> c");

        Assert.Equal("a bold c", Payload<ParagraphPayload>(Assert.Single(blocks)).Spans.ToPlainText());
    }

    [Fact]
    public void Paragraph_SoftBreak_CollapsesToSpace()
    {
        var blocks = Parse("one line\ncontinues here");

        Assert.Equal("one line continues here", Payload<ParagraphPayload>(Assert.Single(blocks)).Spans.ToPlainText());
    }

    [Fact]
    public void Paragraph_HardBreak_SplitsBlocks()
    {
        var blocks = Parse("first  \nsecond");

        Assert.Equal(2, blocks.Count);
        Assert.Equal("first", Payload<ParagraphPayload>(blocks[0]).Spans.ToPlainText());
        Assert.Equal("second", Payload<ParagraphPayload>(blocks[1]).Spans.ToPlainText());
    }

    [Fact]
    public void Paragraph_HardBreakInsideEmphasis_SplitsBlocksKeepingMarks()
    {
        var blocks = Parse("**one  \ntwo**");

        Assert.Equal(2, blocks.Count);
        AssertSpan(Payload<ParagraphPayload>(blocks[0]).Spans, "one", typeof(BoldMark));
        AssertSpan(Payload<ParagraphPayload>(blocks[1]).Spans, "two", typeof(BoldMark));
    }

    [Fact]
    public void Paragraph_Multiple_AreSeparateBlocks()
    {
        var blocks = Parse("one\n\ntwo\n\nthree");

        Assert.Equal(3, blocks.Count);
        Assert.All(blocks, block => Assert.Equal(BlockTypes.Paragraph, block.Type));
        Assert.Equal(["one", "two", "three"], blocks.Select(block => block.PlainText).ToArray());
    }

    #endregion

    #region Headings

    [Fact]
    public void Heading_OffsetShiftsLevel()
    {
        var blocks = Parse("## Section", headingLevelOffset: 1);

        var heading = Payload<HeadingPayload>(Assert.Single(blocks));
        Assert.Equal(1, heading.Level);
        Assert.Equal("Section", heading.Spans.ToPlainText());
    }

    [Theory]
    [InlineData("# H", 2, 1)] // notebook files: # is the notebook title, content clamps to 1
    [InlineData("### H", 2, 1)] // notebook files: ### is content level 1
    [InlineData("##### H", 2, 3)]
    [InlineData("###### H", -1, 6)] // clamped to the payload's 1..6 range
    public void Heading_OffsetAndClamp_MapLevel(string markdown, int offset, int expectedLevel)
    {
        var heading = Payload<HeadingPayload>(Assert.Single(Parse(markdown, offset)));

        Assert.Equal(expectedLevel, heading.Level);
    }

    [Fact]
    public void Heading_Setext_ParsesWithMarks()
    {
        var blocks = Parse("**Bold** title\n===");

        var heading = Payload<HeadingPayload>(Assert.Single(blocks));
        Assert.Equal(1, heading.Level);
        Assert.Equal("Bold title", heading.Spans.ToPlainText());
        AssertSpan(heading.Spans, "Bold", typeof(BoldMark));
    }

    #endregion

    #region Code blocks

    [Fact]
    public void CodeFence_WithoutLanguage_FallsBackToText()
    {
        var blocks = Parse("```\nvar x = 1;\n```");

        var code = Payload<CodePayload>(Assert.Single(blocks));
        Assert.Equal("var x = 1;", code.Code);
        Assert.Equal("text", code.Language);
    }

    [Fact]
    public void CodeFence_LanguageIsFirstTokenOfInfoString()
    {
        var blocks = Parse("```csharp extra\nvar x = 1;\n```");

        Assert.Equal("csharp", Payload<CodePayload>(Assert.Single(blocks)).Language);
    }

    [Fact]
    public void CodeFence_TildeFence_Parses()
    {
        var blocks = Parse("~~~python\nprint(1)\n~~~");

        var code = Payload<CodePayload>(Assert.Single(blocks));
        Assert.Equal("python", code.Language);
        Assert.Equal("print(1)", code.Code);
    }

    [Fact]
    public void CodeFence_BlankLinesAndNewlines_ArePreserved()
    {
        var blocks = Parse("```\na\n\nb\n```");

        var code = Payload<CodePayload>(Assert.Single(blocks));
        Assert.Equal("a\n\nb", code.Code);
        Assert.Equal("a\n\nb", blocks[0].PlainText);
    }

    [Fact]
    public void CodeBlock_Indented_BecomesTextCodeBlock()
    {
        var blocks = Parse("    var x = 1;\n    return x;");

        var code = Payload<CodePayload>(Assert.Single(blocks));
        Assert.Equal("text", code.Language);
        Assert.Equal("var x = 1;\nreturn x;", code.Code);
    }

    #endregion

    #region Dividers

    [Fact]
    public void ThematicBreak_BecomesDivider_ButSetextUnderlineStaysHeading()
    {
        var blocks = Parse("---\n\nTitle\n---", headingLevelOffset: 1);

        Assert.Equal(2, blocks.Count);
        Assert.Equal(BlockTypes.Divider, blocks[0].Type);
        Assert.Equal(BlockTypes.Heading, blocks[1].Type);
        Assert.Equal("Title", Payload<HeadingPayload>(blocks[1]).Spans.ToPlainText());
    }

    [Theory]
    [InlineData("***")]
    [InlineData("___")]
    [InlineData("- - -")]
    public void ThematicBreak_AllForms_BecomeDividerWithEmptyPlainText(string markdown)
    {
        var block = Assert.Single(Parse(markdown));

        Assert.Equal(BlockTypes.Divider, block.Type);
        Assert.Equal(string.Empty, block.PlainText);
    }

    #endregion

    #region Quotes

    [Fact]
    public void Quote_MultiParagraph_BecomesSiblingQuotes()
    {
        var blocks = Parse("> first\n>\n> second");

        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, block => Assert.Equal(BlockTypes.Quote, block.Type));
        Assert.Equal("first", Payload<QuotePayload>(blocks[0]).Spans.ToPlainText());
        Assert.Equal("second", Payload<QuotePayload>(blocks[1]).Spans.ToPlainText());
    }

    [Fact]
    public void Quote_HardBreak_SplitsIntoQuoteBlocks()
    {
        var blocks = Parse("> one  \n> two");

        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, block => Assert.Equal(BlockTypes.Quote, block.Type));
    }

    [Fact]
    public void Quote_NestedQuote_FlattensToQuoteBlocks()
    {
        var blocks = Parse("> > deep");

        var quote = Payload<QuotePayload>(Assert.Single(blocks));
        Assert.Equal("deep", quote.Spans.ToPlainText());
    }

    [Fact]
    public void Quote_ContainingList_LosesQuoteButKeepsContent()
    {
        var blocks = Parse("> - a\n> - b");

        Assert.Equal(2, blocks.Count);
        Assert.Equal(["a", "b"], blocks.Select(block => block.PlainText).ToArray());
        Assert.DoesNotContain(blocks, block => block.Type == BlockTypes.Quote);
    }

    [Fact]
    public void Quote_ContainingCodeBlock_KeepsCode()
    {
        var blocks = Parse("> ```\n> var x = 1;\n> ```");

        var code = Payload<CodePayload>(Assert.Single(blocks));
        Assert.Equal("var x = 1;", code.Code);
    }

    #endregion

    #region Callouts

    [Fact]
    public void Callout_ObsidianSyntax_MapsVariantAndBody()
    {
        var blocks = Parse("> [!warning]\n> Watch **out**");

        var callout = Payload<CalloutPayload>(Assert.Single(blocks));
        Assert.Equal(BlockTypes.Callout, blocks[0].Type);
        Assert.Equal(PaletteColor.Warning, callout.Variant);
        Assert.Equal("Watch out", callout.Spans.ToPlainText());
        AssertSpan(callout.Spans, "out", typeof(BoldMark));
    }

    [Theory]
    [InlineData("primary", PaletteColor.Primary)]
    [InlineData("success", PaletteColor.Success)]
    [InlineData("danger", PaletteColor.Danger)]
    [InlineData("warning", PaletteColor.Warning)]
    [InlineData("info", PaletteColor.Info)]
    [InlineData("muted", PaletteColor.Muted)]
    [InlineData("WARNING", PaletteColor.Warning)] // case-insensitive
    public void Callout_AllPaletteVariants_AreRecognized(string variant, PaletteColor expected)
    {
        var blocks = Parse($"> [!{variant}]\n> body");

        var callout = Payload<CalloutPayload>(Assert.Single(blocks));
        Assert.Equal(BlockTypes.Callout, blocks[0].Type);
        Assert.Equal(expected, callout.Variant);
    }

    [Fact]
    public void Callout_Bodyless_EmitsCalloutWithEmptySpans()
    {
        var blocks = Parse("> [!info]");

        var callout = Payload<CalloutPayload>(Assert.Single(blocks));
        Assert.Equal(BlockTypes.Callout, blocks[0].Type);
        Assert.Equal(PaletteColor.Info, callout.Variant);
        Assert.Equal(string.Empty, callout.Spans.ToPlainText());
    }

    [Fact]
    public void Callout_MultiParagraph_RemainingContentBecomesSiblingBlocks()
    {
        var blocks = Parse("> [!warning]\n> first\n>\n> second");

        Assert.Equal(2, blocks.Count);
        var callout = Payload<CalloutPayload>(blocks[0]);
        Assert.Equal(BlockTypes.Callout, blocks[0].Type);
        Assert.Equal("first", callout.Spans.ToPlainText());
        // The second paragraph is not dropped; it lands as a plain quote sibling.
        Assert.Equal(BlockTypes.Quote, blocks[1].Type);
        Assert.Equal("second", Payload<QuotePayload>(blocks[1]).Spans.ToPlainText());
    }

    [Fact]
    public void Callout_BlankLineAfterMarker_BodyBecomesSiblingQuote()
    {
        var blocks = Parse("> [!info]\n>\n> body");

        Assert.Equal(2, blocks.Count);
        Assert.Equal(BlockTypes.Callout, blocks[0].Type);
        Assert.Equal(string.Empty, Payload<CalloutPayload>(blocks[0]).Spans.ToPlainText());
        Assert.Equal(BlockTypes.Quote, blocks[1].Type);
        Assert.Equal("body", blocks[1].PlainText);
    }

    [Fact]
    public void Callout_BodyWithHardBreak_CollapsesToSingleLine()
    {
        var blocks = Parse("> [!info]\n> a  \n> b");

        var callout = Payload<CalloutPayload>(Assert.Single(blocks));
        Assert.Equal("a b", callout.Spans.ToPlainText());
    }

    [Fact]
    public void Callout_UnknownVariant_StaysQuote()
    {
        var blocks = Parse("> [!sparkles]\n> body");

        Assert.Equal(BlockTypes.Quote, Assert.Single(blocks).Type);
    }

    [Fact]
    public void Callout_TrailingTextOnMarkerLine_StaysQuote()
    {
        var blocks = Parse("> [!note] extra");

        Assert.Equal(BlockTypes.Quote, Assert.Single(blocks).Type);
    }

    #endregion

    #region Lists

    [Fact]
    public void BulletList_NestingBecomesChildren()
    {
        var blocks = Parse("- parent\n  - child");

        var parent = Assert.Single(blocks);
        Assert.Equal(BlockTypes.BulletedList, parent.Type);
        Assert.Equal("parent", parent.PlainText);
        var child = Assert.Single(parent.Children);
        Assert.Equal(BlockTypes.BulletedList, child.Type);
        Assert.Equal("child", child.PlainText);
    }

    [Fact]
    public void BulletList_ThreeLevels_NestRecursively()
    {
        var blocks = Parse("- a\n  - b\n    - c");

        var a = Assert.Single(blocks);
        var b = Assert.Single(a.Children);
        var c = Assert.Single(b.Children);
        Assert.Equal("a", a.PlainText);
        Assert.Equal("b", b.PlainText);
        Assert.Equal("c", c.PlainText);
    }

    [Fact]
    public void OrderedList_BecomesNumberedListItems()
    {
        var blocks = Parse("3. three\n4. four");

        Assert.Equal(2, blocks.Count);
        Assert.Equal(BlockTypes.NumberedList, blocks[0].Type);
        Assert.Equal(BlockTypes.NumberedList, blocks[1].Type);
        // The numbers are not text: numbering is derived from the run at render time.
        Assert.Equal("three", blocks[0].PlainText);
        Assert.Equal("four", blocks[1].PlainText);
    }

    [Fact]
    public void OrderedList_ParenDelimiter_Parses()
    {
        var blocks = Parse("1) one\n2) two");

        Assert.Equal(2, blocks.Count);
        Assert.Equal(BlockTypes.NumberedList, blocks[0].Type);
        Assert.Equal("one", blocks[0].PlainText);
        Assert.Equal("two", blocks[1].PlainText);
    }

    [Fact]
    public void ListItem_MultipleParagraphs_ExtraParagraphsBecomeChildren()
    {
        var blocks = Parse("- first\n\n  second");

        var item = Assert.Single(blocks);
        Assert.Equal("first", item.PlainText);
        var child = Assert.Single(item.Children);
        Assert.Equal("second", child.PlainText);
    }

    [Fact]
    public void ListItem_WithCodeBlock_CodeBecomesChild()
    {
        var blocks = Parse("- item\n\n  ```\n  var x = 1;\n  ```");

        var item = Assert.Single(blocks);
        var child = Assert.Single(item.Children);
        Assert.Equal(BlockTypes.Code, child.Type);
        Assert.Equal("var x = 1;", Payload<CodePayload>(child).Code);
    }

    [Fact]
    public void TaskList_MapsCheckedState()
    {
        var blocks = Parse("- [ ] open\n- [x] done");

        Assert.Equal(2, blocks.Count);
        var open = Payload<TodoPayload>(blocks[0]);
        var done = Payload<TodoPayload>(blocks[1]);
        Assert.False(open.Checked);
        Assert.True(done.Checked);
        Assert.Equal("open", open.Spans.ToPlainText());
        Assert.Equal("done", done.Spans.ToPlainText());
    }

    [Fact]
    public void TaskList_NestedTask_BecomesChildTodo()
    {
        var blocks = Parse("- [ ] parent\n  - [x] child");

        var parent = Assert.Single(blocks);
        Assert.Equal(BlockTypes.Todo, parent.Type);
        var child = Assert.Single(parent.Children);
        Assert.Equal(BlockTypes.Todo, child.Type);
        Assert.True(Payload<TodoPayload>(child).Checked);
    }

    [Fact]
    public void OrderedTaskList_StaysPlainTodo()
    {
        var blocks = Parse("1. [ ] task");

        var todo = Payload<TodoPayload>(Assert.Single(blocks));
        Assert.Equal("task", todo.Spans.ToPlainText());
    }

    #endregion

    #region Tables

    [Fact]
    public void PipeTable_MapsHeaderRowsAndAlignments()
    {
        var blocks = Parse("| Name | Qty |\n| :--- | ---: |\n| apples | 3 |");

        var table = Payload<TablePayload>(Assert.Single(blocks));
        Assert.Equal([TableColumnAlignment.Left, TableColumnAlignment.Right], table.Alignments);
        var header = Assert.IsAssignableFrom<IReadOnlyList<Spans>>(table.Header);
        Assert.Equal("Name", header[0].ToPlainText());
        Assert.Equal("Qty", header[1].ToPlainText());
        var row = Assert.Single(table.Rows);
        Assert.Equal("apples", row[0].ToPlainText());
        Assert.Equal("3", row[1].ToPlainText());
    }

    [Fact]
    public void PipeTable_WithoutAlignmentMarkers_DefaultsToNone()
    {
        var blocks = Parse("| A |\n| --- |\n| 1 |");

        var table = Payload<TablePayload>(Assert.Single(blocks));
        Assert.Equal([TableColumnAlignment.None], table.Alignments);
    }

    [Fact]
    public void PipeTable_CenterAlignment_Maps()
    {
        var blocks = Parse("| A |\n| :-: |\n| 1 |");

        Assert.Equal([TableColumnAlignment.Center], Payload<TablePayload>(Assert.Single(blocks)).Alignments);
    }

    [Fact]
    public void PipeTable_CellInlineMarks_ArePreserved()
    {
        var blocks = Parse("| **bold** |\n| --- |\n| `code` |");

        var table = Payload<TablePayload>(Assert.Single(blocks));
        var header = Assert.IsAssignableFrom<IReadOnlyList<Spans>>(table.Header);
        AssertSpan(header[0], "bold", typeof(BoldMark));
        AssertSpan(Assert.Single(table.Rows)[0], "code", typeof(CodeMark));
    }

    [Fact]
    public void PipeTable_RaggedRow_IsPaddedToColumnCount()
    {
        var blocks = Parse("| A | B |\n| --- | --- |\n| only |");

        var table = Payload<TablePayload>(Assert.Single(blocks));
        Assert.Equal(2, Assert.Single(table.Rows).Count);
    }

    [Fact]
    public void PipeTable_ExcessCells_AreTruncatedToColumnCount()
    {
        var blocks = Parse("| A |\n| --- |\n| 1 | 2 |");

        var table = Payload<TablePayload>(Assert.Single(blocks));
        var cell = Assert.Single(Assert.Single(table.Rows));
        Assert.Equal("1", cell.ToPlainText());
    }

    [Fact]
    public void PipeTable_PlainText_JoinsHeaderAndCells()
    {
        var blocks = Parse("| Name | Qty |\n| --- | --- |\n| apples | 3 |");

        Assert.Equal("Name Qty apples 3", Assert.Single(blocks).PlainText);
    }

    #endregion

    #region Images

    [Fact]
    public void Image_StandaloneParagraph_BecomesImageBlock()
    {
        var blocks = Parse("![a cat](https://example.com/cat.png)");

        var image = Payload<ImagePayload>(Assert.Single(blocks));
        Assert.Equal("https://example.com/cat.png", image.Url);
        Assert.Equal("a cat", image.Alt);
        Assert.False(image.IsDecorative);
        Assert.Null(image.Caption);
    }

    [Fact]
    public void Image_WithoutAlt_IsDecorative()
    {
        var blocks = Parse("![](https://example.com/cat.png)");

        var image = Payload<ImagePayload>(Assert.Single(blocks));
        Assert.Null(image.Alt);
        Assert.True(image.IsDecorative);
    }

    [Fact]
    public void Image_AltWithInlineCode_PreservesCodeText()
    {
        var blocks = Parse("![see `code` here](https://example.com/x.png)");

        var image = Payload<ImagePayload>(Assert.Single(blocks));
        Assert.Equal("see code here", image.Alt);
        Assert.Equal("see code here", blocks[0].PlainText);
    }

    [Fact]
    public void Image_InlineWithinText_KeepsAltAsText()
    {
        var blocks = Parse("look ![a cat](https://example.com/cat.png) here");

        var paragraph = Assert.Single(blocks);
        Assert.Equal(BlockTypes.Paragraph, paragraph.Type);
        Assert.Equal("look a cat here", paragraph.PlainText);
    }

    [Fact]
    public void Image_EmptyUrl_StaysParagraphWithAltText()
    {
        var blocks = Parse("![alt]()");

        var paragraph = Assert.Single(blocks);
        Assert.Equal(BlockTypes.Paragraph, paragraph.Type);
        Assert.Equal("alt", paragraph.PlainText);
    }

    [Fact]
    public void ImageCaption_ItalicOnlyParagraph_ReattachesToImage()
    {
        var blocks = Parse("![a cat](https://example.com/cat.png)\n_A fuzzy one_");

        var image = Payload<ImagePayload>(Assert.Single(blocks));
        Assert.Equal("A fuzzy one", image.Caption);
    }

    [Fact]
    public void ImageCaption_WithInlineCode_PreservesCodeText()
    {
        var blocks = Parse("![alt](https://example.com/x.png)\n_run `dotnet test`_");

        var image = Payload<ImagePayload>(Assert.Single(blocks));
        Assert.Equal("run dotnet test", image.Caption);
    }

    [Fact]
    public void ImageCaption_BoldTextAfterSoftBreak_KeepsImageInline()
    {
        // Only an italic-only line reattaches as a caption; bold text keeps the whole paragraph
        // inline, with the image degrading to its alt text.
        var blocks = Parse("![alt](https://example.com/x.png)\n**not a caption**");

        var paragraph = Assert.Single(blocks);
        Assert.Equal(BlockTypes.Paragraph, paragraph.Type);
        Assert.Equal("alt not a caption", paragraph.PlainText);
        AssertSpan(Payload<ParagraphPayload>(paragraph).Spans, "not a caption", typeof(BoldMark));
    }

    [Fact]
    public void ImageCaption_PlainTextAfterSoftBreak_KeepsImageInline()
    {
        var blocks = Parse("![alt](https://example.com/x.png)\njust text");

        var paragraph = Assert.Single(blocks);
        Assert.Equal(BlockTypes.Paragraph, paragraph.Type);
        Assert.Equal("alt just text", paragraph.PlainText);
    }

    [Fact]
    public void ImageCaption_InSeparateParagraph_DoesNotReattach()
    {
        var blocks = Parse("![alt](https://example.com/x.png)\n\n_italic but separate_");

        Assert.Equal(2, blocks.Count);
        Assert.Equal(BlockTypes.Image, blocks[0].Type);
        Assert.Null(Payload<ImagePayload>(blocks[0]).Caption);
        Assert.Equal(BlockTypes.Paragraph, blocks[1].Type);
    }

    #endregion

    #region HTML

    [Fact]
    public void HtmlBlock_StripsTagsKeepsText()
    {
        var blocks = Parse("<div>bare text</div>");

        Assert.Equal("bare text", Assert.Single(blocks).PlainText);
    }

    [Fact]
    public void HtmlBlock_MultiLine_KeepsInnerText()
    {
        var blocks = Parse("<div>\nhello\n</div>");

        Assert.Equal("hello", Assert.Single(blocks).PlainText);
    }

    [Fact]
    public void HtmlBlock_WhitespaceOnly_EmitsNothing()
    {
        var blocks = Parse("<div>\n</div>");

        Assert.Empty(blocks);
    }

    #endregion

    #region Autolinks

    [Fact]
    public void Autolink_AngleBracketUrl_BecomesLinkMark()
    {
        var blocks = Parse("<https://example.com>");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        AssertSpan(spans, "https://example.com", typeof(LinkMark));
    }

    [Fact]
    public void Autolink_BareUrl_BecomesLinkMark()
    {
        var blocks = Parse("see https://example.com now");

        var spans = Payload<ParagraphPayload>(Assert.Single(blocks)).Spans;
        Assert.Equal("see https://example.com now", spans.ToPlainText());
        AssertSpan(spans, "https://example.com", typeof(LinkMark));
    }

    #endregion

    #region Document level

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n\n")]
    public void Document_WithoutContent_EmitsNothing(string markdown)
    {
        Assert.Empty(Parse(markdown));
    }

    [Fact]
    public void Document_MixedContent_PreservesOrder()
    {
        var blocks = Parse("# Title\n\ntext\n\n---\n\n```\nx\n```\n\n> quoted");

        Assert.Equal(
            [BlockTypes.Heading, BlockTypes.Paragraph, BlockTypes.Divider, BlockTypes.Code, BlockTypes.Quote],
            blocks.Select(block => block.Type).ToArray());
    }

    [Fact]
    public void Document_FootnoteSyntax_DegradesToTextWithoutBrackets()
    {
        // No footnotes extension: `[^1]` parses as a reference link to "^1" (a relative URL, so
        // the link mark drops and the brackets vanish); the definition is consumed as a link
        // reference definition and emits no block. The note text itself is lost.
        var blocks = Parse("text[^1]\n\n[^1]: note");

        var block = Assert.Single(blocks);
        Assert.Equal("text^1", block.PlainText);
    }

    #endregion

    private static void AssertSpan(Spans spans, string text, Type markType)
    {
        var span = spans.Items.SingleOrDefault(candidate => candidate.Text == text);
        Assert.NotNull(span);
        Assert.Contains(span.Marks, mark => mark.GetType() == markType);
    }
}
