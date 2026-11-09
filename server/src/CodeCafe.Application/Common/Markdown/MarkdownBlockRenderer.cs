using System.Text;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Common.Markdown;

// Renders a page's block tree as markdown content, shared by the notebook and page exporters.
// headingHashOffset shifts block heading levels up to markdown hashes: 2 for notebook files
// (# = notebook, ## = page), 1 for page files (# = page title). The importers' matching
// headingLevelOffset round-trips the level.
//
// Children are emitted as indented lines. Only list nesting has a markdown shape the importer can
// rebuild; a child of any other block re-imports as continuation text of its parent, and marks
// that only exist as HTML tags (underline, kbd, sup/sub, abbr) lose their marking to tag stripping.
internal static class MarkdownBlockRenderer
{
    public static void RenderBlocks(StringBuilder output, Page page, IReadOnlyList<Block> blocks, int headingHashOffset)
    {
        // A numbered item's number is its position in the run of consecutive
        // numbered siblings — computed per group, never stored.
        var numbers = new Dictionary<Guid, int>();
        var pending = new Stack<(Block Block, int Depth)>();
        PushBlocks(pending, page, blocks, null, 0, numbers);

        while (pending.Count > 0)
        {
            var (block, depth) = pending.Pop();
            RenderBlock(output, block, depth, headingHashOffset, numbers);
            PushBlocks(pending, page, blocks, block.Id, depth + 1, numbers);
        }
    }

    private static void PushBlocks(
        Stack<(Block Block, int Depth)> pending,
        Page page,
        IReadOnlyList<Block> blocks,
        Guid? parentId,
        int depth,
        Dictionary<Guid, int> numbers
    )
    {
        var siblings = BlockChain.OrderByChain(page, blocks, parentId);
        var run = 0;
        foreach (var sibling in siblings)
        {
            run = sibling.Type == BlockTypes.NumberedList ? run + 1 : 0;
            if (sibling.Type == BlockTypes.NumberedList)
            {
                numbers[sibling.Id] = run;
            }
        }
        for (var index = siblings.Count - 1; index >= 0; index--)
        {
            pending.Push((siblings[index], depth));
        }
    }

    private static void RenderBlock(
        StringBuilder output,
        Block block,
        int depth,
        int headingHashOffset,
        IReadOnlyDictionary<Guid, int> numbers
    )
    {
        var indent = new string(' ', depth * 2);
        switch (block.Type)
        {
            case BlockTypes.Paragraph:
                output.Append(indent).AppendLine(RenderMultiline(GetPayload<ParagraphPayload>(block).Spans));
                break;
            case BlockTypes.Heading:
                {
                    var payload = GetPayload<HeadingPayload>(block);
                    // ATX headings are single-line; soft breaks degrade to spaces.
                    output.Append(indent).Append(new string('#', Math.Min(payload.Level + headingHashOffset, 6))).Append(' ')
                        .AppendLine(RenderSpans(payload.Spans).Replace("\n", " "));
                    break;
                }
            case BlockTypes.Todo:
                {
                    var payload = GetPayload<TodoPayload>(block);
                    output.Append(indent).Append(payload.Checked ? "- [x] " : "- [ ] ")
                        .AppendLine(RenderMultiline(payload.Spans));
                    break;
                }
            case BlockTypes.BulletedList:
                output.Append(indent).Append("- ").AppendLine(RenderMultiline(GetPayload<ListItemPayload>(block).Spans));
                break;
            case BlockTypes.NumberedList:
                output.Append(indent).Append(numbers.GetValueOrDefault(block.Id, 1)).Append(". ")
                    .AppendLine(RenderMultiline(GetPayload<ListItemPayload>(block).Spans));
                break;
            case BlockTypes.Code:
                {
                    var payload = GetPayload<CodePayload>(block);
                    output.Append(indent).Append("```").AppendLine(payload.Language);
                    output.AppendLine(payload.Code);
                    output.Append(indent).AppendLine("```");
                    break;
                }
            case BlockTypes.Quote:
                RenderQuote(output, indent, RenderMultiline(GetPayload<QuotePayload>(block).Spans));
                break;
            case BlockTypes.Callout:
                {
                    var payload = GetPayload<CalloutPayload>(block);
                    RenderQuote(output, indent, $"[!{payload.Variant.ToString().ToUpperInvariant()}]\n{RenderMultiline(payload.Spans)}");
                    break;
                }
            case BlockTypes.Divider:
                output.Append(indent).AppendLine("---");
                break;
            case BlockTypes.Table:
                RenderTable(output, indent, GetPayload<TablePayload>(block));
                break;
            case BlockTypes.Image:
                {
                    var payload = GetPayload<ImagePayload>(block);
                    output.Append(indent).Append("![").Append(EscapeText(payload.Alt ?? string.Empty)).Append("](")
                        .Append(EscapeUrl(payload.Url)).AppendLine(")");
                    if (!string.IsNullOrWhiteSpace(payload.Caption))
                    {
                        output.Append(indent).Append("_").Append(EscapeText(payload.Caption)).AppendLine("_");
                    }

                    break;
                }
            case BlockTypes.Audio:
                {
                    var payload = GetPayload<AudioPayload>(block);
                    output.Append(indent).Append("[audio](").Append(EscapeUrl(payload.Url)).AppendLine(")");
                    break;
                }
            default:
                output.Append(indent).Append("<!-- unsupported block: ").Append(EscapeText(block.Type)).AppendLine(" -->");
                break;
        }

        output.AppendLine();
    }

    private static void RenderTable(StringBuilder output, string indent, TablePayload payload)
    {
        // GFM pipe tables must have a header row; a headerless table exports an empty one.
        var header = payload.Header ?? payload.Alignments.Select(_ => Spans.Empty).ToList();
        RenderTableRow(output, indent, header);

        output.Append(indent).Append('|');
        foreach (var alignment in payload.Alignments)
        {
            output.Append(alignment switch
            {
                TableColumnAlignment.Left => " :--- |",
                TableColumnAlignment.Center => " :---: |",
                TableColumnAlignment.Right => " ---: |",
                _ => " --- |",
            });
        }

        output.AppendLine();
        foreach (var row in payload.Rows)
        {
            RenderTableRow(output, indent, row);
        }
    }

    private static void RenderTableRow(StringBuilder output, string indent, IReadOnlyList<Spans> cells)
    {
        output.Append(indent).Append('|');
        foreach (var cell in cells)
        {
            // A bare | would end the cell. The escape is unreliable inside code spans (GFM
            // parses them first), a known edge the importer accepts as a parse artifact.
            // Pipe-table rows are single-line, so soft breaks degrade to spaces.
            output.Append(' ').Append(RenderSpans(cell).Replace("\n", " ").Replace("|", "\\|", StringComparison.Ordinal)).Append(" |");
        }

        output.AppendLine();
    }

    private static void RenderQuote(StringBuilder output, string indent, string text)
    {
        foreach (var line in text.Split('\n'))
        {
            output.Append(indent).Append("> ").AppendLine(line);
        }
    }

    private static string RenderSpans(Spans spans)
        => string.Concat(spans.Items.Select(RenderSpan));

    // Soft line breaks export as markdown hard breaks (backslash-newline); the
    // continuation line re-imports as a lazy continuation of the same block.
    private static string RenderMultiline(Spans spans)
    {
        var text = RenderSpans(spans).Replace("\n", "\\\n");
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = EscapeLineStart(lines[i]);
        }

        return string.Join('\n', lines);
    }

    // Any line — the first included — re-imports as a lazy continuation of the same block unless
    // it opens with a block-level marker, which would interrupt the block and change its type: a
    // paragraph starting "- " becomes a list, "> " a quote, "# " a heading, and inside a notebook
    // file a stray "#" line even re-splits the document. EscapeText already neutralizes '*' and
    // backticks; handle the rest. The backslash is a CommonMark escape before punctuation, so the
    // reader sees the original characters.
    private static string EscapeLineStart(string line)
    {
        if (line.Length == 0)
        {
            return line;
        }
        if (line[0] is '#' or '>' or '<' or '~' or '=' or '-' or '+')
        {
            return "\\" + line;
        }
        var digits = 0;
        while (digits < line.Length && char.IsAsciiDigit(line[digits]))
        {
            digits++;
        }
        if (digits > 0 && digits < line.Length && line[digits] is '.' or ')')
        {
            return $"{line[..digits]}\\{line[digits]}{line[(digits + 1)..]}";
        }

        return line;
    }

    // Marks arrive in the domain's canonical order, which is outside-in (link, code, sup/sub,
    // then styles), so the content is built inside-out: the last mark wraps the raw text and each
    // earlier one wraps the result. Applying them front to back would invert the nesting and, for
    // a code span, fence the emphasis syntax instead of the text.
    private static string RenderSpan(Span span)
    {
        // A code span is a literal context: markdown escapes mean nothing inside it, so its text
        // is fenced raw. Markdown can only nest code *inside* other marks, so the fence is always
        // the innermost layer whatever the canonical position of the code mark.
        var hasCode = span.Marks.Any(mark => mark is CodeMark);
        var text = hasCode ? RenderCodeSpan(span.Text) : EscapeText(span.Text);
        for (var index = span.Marks.Count - 1; index >= 0; index--)
        {
            text = span.Marks[index] switch
            {
                CodeMark => text,
                BoldMark => $"**{text}**",
                ItalicMark => $"*{text}*",
                UnderlineMark => $"<u>{text}</u>",
                StrikeMark => $"~~{text}~~",
                LinkMark link => $"[{text}]({EscapeUrl(link.Href)})",
                KbdMark => $"<kbd>{text}</kbd>",
                SupMark => $"<sup>{text}</sup>",
                SubMark => $"<sub>{text}</sub>",
                AbbrMark abbr => $"<abbr title=\"{EscapeAttribute(abbr.Title)}\">{text}</abbr>",
                ColorMark or HighlightMark => text,
                _ => text,
            };
        }

        return text;
    }

    // A code span's fence has to be longer than any backtick run inside it, and content that
    // starts or ends with a backtick needs a space on both sides so it cannot touch the fence
    // (CommonMark strips that pair again, so the text survives unchanged).
    private static string RenderCodeSpan(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        var longestRun = 0;
        var run = 0;
        foreach (var character in text)
        {
            run = character == '`' ? run + 1 : 0;
            longestRun = Math.Max(longestRun, run);
        }

        var fence = new string('`', longestRun + 1);
        var pad = text[0] == '`' || text[^1] == '`' ? " " : string.Empty;
        return $"{fence}{pad}{text}{pad}{fence}";
    }

    private static TPayload GetPayload<TPayload>(Block block)
        => BlockPayloads.DeserializeForRead<TPayload>(block.ContentJson);

    internal static string EscapeText(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal);

    private static string EscapeUrl(string value) => value.Replace(")", "%29", StringComparison.Ordinal);

    private static string EscapeAttribute(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
