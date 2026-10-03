using System.Text;

using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Domain.Blocks;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using MdBlock = Markdig.Syntax.Block;

namespace CodeCafe.Application.Common.Markdown;

// Maps a Markdig block tree onto our block model. The parser (Markdig) owns CommonMark/GFM
// correctness — escapes, nested emphasis, setext-vs-thematic-break ambiguity; this class owns
// only the mapping. Anything markdown can express must land somewhere (we aim to be a
// superset); the deliberate degradations are commented where they happen.
internal static class MarkdownDocumentParser
{
    // Selective extensions: pipe tables, task lists, emphasis extras (~~strike~~, ~sub~, ^sup^,
    // ==highlight==, ++inserted++) and autolinks. Footnotes and definition lists stay literal
    // text rather than being silently restructured.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseTaskLists()
        .UseEmphasisExtras()
        .UseAutoLinks()
        .Build();

    public static MarkdownDocument ParseDocument(string markdown) => Markdig.Markdown.Parse(markdown, Pipeline);

    // headingLevelOffset shifts markdown heading levels down to block levels: 2 for notebook
    // files (# = notebook, ## = page), 1 for page files (# = page title).
    public static IReadOnlyList<ParsedMarkdownBlock> ParseBlocks(IEnumerable<MdBlock> blocks, int headingLevelOffset)
    {
        var list = blocks as IReadOnlyList<MdBlock> ?? blocks.ToList();
        var output = new List<ParsedMarkdownBlock>();
        foreach (var block in list)
        {
            AppendBlock(output, block, headingLevelOffset);
        }

        return output;
    }

    private static void AppendBlock(List<ParsedMarkdownBlock> output, MdBlock block, int headingLevelOffset)
    {
        switch (block)
        {
            case HeadingBlock heading:
                var level = Math.Clamp(heading.Level - headingLevelOffset, 1, 6);
                foreach (var spans in MarkdownInlineParser.Parse(heading.Inline))
                {
                    output.Add(SpanBlock(BlockTypes.Heading, new HeadingPayload { Level = level, Spans = spans }, spans));
                }

                break;
            case ParagraphBlock paragraph:
                AppendParagraph(output, paragraph);
                break;
            case FencedCodeBlock fenced:
                // The info string's first token is the language; CodePayload rejects empty, so
                // bare fences become "text".
                var language = fenced.Info?.Trim().Split(' ', 2)[0] ?? string.Empty;
                output.Add(new ParsedMarkdownBlock(
                    BlockTypes.Code,
                    BlockPayloads.SerializeForWrite(new CodePayload
                    {
                        Code = LinesText(fenced),
                        Language = string.IsNullOrEmpty(language) ? "text" : language,
                    }),
                    LinesText(fenced),
                    []));
                break;
            case CodeBlock indented:
                var code = LinesText(indented);
                output.Add(new ParsedMarkdownBlock(
                    BlockTypes.Code,
                    BlockPayloads.SerializeForWrite(new CodePayload { Code = code, Language = "text" }),
                    code,
                    []));
                break;
            case ThematicBreakBlock:
                output.Add(new ParsedMarkdownBlock(
                    BlockTypes.Divider,
                    BlockPayloads.SerializeForWrite(new DividerPayload()),
                    string.Empty,
                    []));
                break;
            case QuoteBlock quote:
                AppendQuote(output, quote, headingLevelOffset);
                break;
            case ListBlock list:
                AppendList(output, list, headingLevelOffset);
                break;
            case Table table:
                output.Add(ParseTable(table));
                break;
            case HtmlBlock html:
                // Raw HTML blocks keep their inner text and lose their tags.
                var text = StripTags(LinesText(html));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var spans = Spans.Create([new Span(text.Trim(), [])]);
                    output.Add(SpanBlock(BlockTypes.Paragraph, new ParagraphPayload { Spans = spans }, spans));
                }

                break;
            // LinkReferenceDefinitionGroup, EmptyBlock and friends carry no content.
        }
    }

    private static void AppendParagraph(List<ParsedMarkdownBlock> output, ParagraphBlock paragraph)
    {
        // A paragraph holding exactly one image — optionally followed by a soft break and an
        // italic-only caption, which is how the exporter emits captions — is an Image block.
        if (paragraph.Inline?.FirstChild is LinkInline { IsImage: true } image
            && !string.IsNullOrWhiteSpace(image.Url))
        {
            string? caption = null;
            var isStandalone = image.NextSibling is null;
            if (!isStandalone
                && image.NextSibling is LineBreakInline { IsHard: false } lineBreak
                && lineBreak.NextSibling is EmphasisInline { DelimiterChar: '*' or '_', DelimiterCount: 1 } emphasis
                && emphasis.NextSibling is null)
            {
                var captionText = PlainTextOf(emphasis);
                if (!string.IsNullOrWhiteSpace(captionText))
                {
                    caption = captionText;
                    isStandalone = true;
                }
            }

            if (isStandalone)
            {
                var alt = PlainTextOf(image);
                output.Add(new ParsedMarkdownBlock(
                    BlockTypes.Image,
                    BlockPayloads.SerializeForWrite(new ImagePayload
                    {
                        Url = image.Url,
                        Alt = string.IsNullOrWhiteSpace(alt) ? null : alt,
                        IsDecorative = string.IsNullOrWhiteSpace(alt),
                        Caption = caption,
                    }),
                    string.IsNullOrWhiteSpace(alt) ? caption ?? string.Empty : alt,
                    []));
                return;
            }
        }

        foreach (var spans in MarkdownInlineParser.Parse(paragraph.Inline))
        {
            output.Add(SpanBlock(BlockTypes.Paragraph, new ParagraphPayload { Spans = spans }, spans));
        }
    }

    private static void AppendQuote(List<ParsedMarkdownBlock> output, QuoteBlock quote, int headingLevelOffset)
    {
        IEnumerable<MdBlock> children = quote;
        if (TryParseCallout(quote, out var callout))
        {
            output.Add(callout);
            // The callout consumed the marker paragraph only; later quote content still lands
            // as sibling blocks rather than vanishing into the callout's single-line payload.
            children = children.Skip(1);
        }

        foreach (var child in children)
        {
            if (child is ParagraphBlock paragraph)
            {
                foreach (var spans in MarkdownInlineParser.Parse(paragraph.Inline))
                {
                    output.Add(SpanBlock(BlockTypes.Quote, new QuotePayload { Spans = spans }, spans));
                }
            }
            else
            {
                // Lists/code inside a quote lose the quote relation but not their content.
                AppendBlock(output, child, headingLevelOffset);
            }
        }
    }

    // "> [!warning]\n> body" — Obsidian-style, which is also what our exporter emits. Foreign
    // quote blocks whose first line is not a known variant stay plain quotes.
    private static bool TryParseCallout(QuoteBlock quote, out ParsedMarkdownBlock callout)
    {
        callout = null!;
        if (quote.FirstOrDefault() is not ParagraphBlock paragraph)
        {
            return false;
        }

        // The "[!variant]" marker parses as two literals ("[" and "!variant]") because "["
        // opens a link that never resolves; join leading literals up to the first line break.
        var marker = new StringBuilder();
        var cursor = paragraph.Inline?.FirstChild;
        while (cursor is LiteralInline literal)
        {
            marker.Append(literal.Content.ToString());
            cursor = cursor.NextSibling;
        }

        var markerText = marker.ToString().Trim();
        if (markerText.Length < 4
            || !markerText.StartsWith("[!", StringComparison.Ordinal)
            || !markerText.EndsWith(']')
            || !Enum.TryParse<PaletteColor>(markerText[2..^1], ignoreCase: true, out var variant))
        {
            return false;
        }

        // After the marker only a line break (body follows) or nothing (bodyless callout like
        // "> [!info]") is legal; trailing text on the marker line means it was never a callout.
        if (cursor is not null and not LineBreakInline)
        {
            return false;
        }

        var spans = cursor is LineBreakInline lineBreak
            ? MarkdownInlineParser.ParseSingleLine(lineBreak.NextSibling)
            : Spans.Empty;
        callout = SpanBlock(BlockTypes.Callout, new CalloutPayload { Variant = variant, Spans = spans }, spans);
        return true;
    }

    private static void AppendList(List<ParsedMarkdownBlock> output, ListBlock list, int headingLevelOffset)
    {
        var number = 1;
        if (list.IsOrdered && int.TryParse(list.OrderedStart, out var start))
        {
            number = start;
        }

        foreach (var item in list.Cast<ListItemBlock>())
        {
            output.Add(ParseListItem(item, list.IsOrdered ? number++ : null, headingLevelOffset));
        }
    }

    private static ParsedMarkdownBlock ParseListItem(ListItemBlock item, int? orderedNumber, int headingLevelOffset)
    {
        // The item's first paragraph becomes the item's own block; nested lists and anything
        // else become its children. Extra hard-break segments join the children.
        ParsedMarkdownBlock? first = null;
        var children = new List<ParsedMarkdownBlock>();

        foreach (var child in item)
        {
            if (child is ParagraphBlock paragraph)
            {
                var task = paragraph.Inline?.FirstChild as Markdig.Extensions.TaskLists.TaskList;
                var segments = MarkdownInlineParser.Parse(paragraph.Inline);
                if (task is not null)
                {
                    // The "[ ] " marker leaves one leading space on the item's first literal.
                    segments = StripOneLeadingSpace(segments);
                }
                for (var i = 0; i < segments.Count; i++)
                {
                    var spans = segments[i];
                    // Ordered lists have no list block type yet; the number survives as literal
                    // text rather than vanishing.
                    if (orderedNumber is { } n && first is null && i == 0)
                    {
                        spans = Spans.Create([new Span($"{n}. ", []), .. spans.Items]);
                    }

                    var block = task is not null
                        ? SpanBlock(BlockTypes.Todo, new TodoPayload { Checked = task.Checked, Spans = spans }, spans)
                        : SpanBlock(BlockTypes.Paragraph, new ParagraphPayload { Spans = spans }, spans);
                    if (first is null)
                    {
                        first = block;
                    }
                    else
                    {
                        children.Add(block);
                    }
                }
            }
            else if (child is ListBlock nested)
            {
                AppendList(children, nested, headingLevelOffset);
            }
            else
            {
                AppendBlock(children, child, headingLevelOffset);
            }
        }

        first ??= SpanBlock(
            BlockTypes.Paragraph,
            new ParagraphPayload { Spans = Spans.Empty },
            Spans.Empty);
        return first with { Children = children };
    }

    private static IReadOnlyList<Spans> StripOneLeadingSpace(IReadOnlyList<Spans> segments)
    {
        if (segments.Count == 0 || segments[0].Items.Count == 0 || !segments[0].Items[0].Text.StartsWith(' '))
        {
            return segments;
        }

        var first = segments[0];
        var trimmed = Spans.Create([first.Items[0] with { Text = first.Items[0].Text[1..] }, .. first.Items.Skip(1)]);
        return [trimmed, .. segments.Skip(1)];
    }

    private static ParsedMarkdownBlock ParseTable(Table table)
    {
        var alignments = table.ColumnDefinitions
            .Select(definition => definition.Alignment switch
            {
                TableColumnAlign.Left => TableColumnAlignment.Left,
                TableColumnAlign.Center => TableColumnAlignment.Center,
                TableColumnAlign.Right => TableColumnAlignment.Right,
                _ => TableColumnAlignment.None,
            })
            .ToList();

        IReadOnlyList<Spans>? header = null;
        var rows = new List<IReadOnlyList<Spans>>();
        foreach (var row in table.Cast<TableRow>())
        {
            var cells = row.Cast<TableCell>().Select(CellSpans).ToList();
            // Malformed markdown can produce ragged rows; pad/truncate to the column count so
            // the payload always satisfies its rectangle invariant.
            while (cells.Count < alignments.Count)
            {
                cells.Add(Spans.Empty);
            }

            if (cells.Count > alignments.Count)
            {
                cells = cells.Take(alignments.Count).ToList();
            }

            if (row.IsHeader)
            {
                header = cells;
            }
            else
            {
                rows.Add(cells);
            }
        }

        var payload = new TablePayload { Alignments = alignments, Header = header, Rows = rows };
        var plainText = string.Join(
            ' ',
            (header ?? []).Concat(rows.SelectMany(row => row)).Select(cell => cell.ToPlainText()));
        return new ParsedMarkdownBlock(BlockTypes.Table, BlockPayloads.SerializeForWrite(payload), plainText, []);
    }

    private static Spans CellSpans(TableCell cell)
    {
        // Cells are single-line: multiple paragraphs join with a space.
        var parts = cell.OfType<ParagraphBlock>()
            .Select(paragraph => MarkdownInlineParser.ParseSingleLine(paragraph.Inline))
            .ToList();
        if (parts.Count == 0)
        {
            return Spans.Empty;
        }

        if (parts.Count == 1)
        {
            return parts[0];
        }

        var spans = new List<Span>();
        foreach (var part in parts)
        {
            if (spans.Count > 0)
            {
                spans.Add(new Span(" ", []));
            }

            spans.AddRange(part.Items);
        }

        return Spans.Create(spans);
    }

    private static ParsedMarkdownBlock SpanBlock<TPayload>(string type, TPayload payload, Spans spans)
        where TPayload : class
        => new(type, BlockPayloads.SerializeForWrite(payload), spans.ToPlainText(), []);

    private static string PlainTextOf(ContainerInline container)
    {
        var builder = new StringBuilder();
        for (var inline = container.FirstChild; inline is not null; inline = inline.NextSibling)
        {
            switch (inline)
            {
                case Markdig.Syntax.Inlines.LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case ContainerInline nested:
                    builder.Append(PlainTextOf(nested));
                    break;
            }
        }

        return builder.ToString();
    }

    private static string LinesText(LeafBlock block)
    {
        var lines = block.Lines;
        if (lines.Lines is null || lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(lines.Lines[i].Slice.ToString());
        }

        return builder.ToString();
    }

    private static string StripTags(string html)
    {
        var builder = new StringBuilder(html.Length);
        var depth = 0;
        foreach (var character in html)
        {
            switch (character)
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    depth = Math.Max(0, depth - 1);
                    break;
                default:
                    if (depth == 0)
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }
}
