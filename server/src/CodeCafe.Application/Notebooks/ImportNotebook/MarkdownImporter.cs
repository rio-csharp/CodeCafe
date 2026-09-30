using System.Text;
using System.Text.Json;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Notebooks.ImportNotebook;

internal static class MarkdownImporter
{
    public static ParsedNotebook Parse(string fileName, string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var title = Path.GetFileNameWithoutExtension(fileName).Trim();
        var pages = new List<ParsedPage>();
        ParsedPage? currentPage = null;
        var blocks = new List<ParsedBlock>();
        var index = 0;

        while (index < lines.Length)
        {
            var line = lines[index];
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                title = line[2..].Trim();
                index++;
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (currentPage is not null)
                {
                    currentPage.Blocks.AddRange(blocks);
                }

                currentPage = new ParsedPage(line[3..].Trim());
                pages.Add(currentPage);
                blocks = [];
                index++;
                continue;
            }

            if (currentPage is null)
            {
                index++;
                continue;
            }

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                var language = line[3..].Trim();
                var code = new StringBuilder();
                index++;
                while (index < lines.Length && !lines[index].StartsWith("```", StringComparison.Ordinal))
                {
                    if (code.Length > 0)
                    {
                        code.AppendLine();
                    }

                    code.Append(lines[index]);
                    index++;
                }

                if (index < lines.Length)
                {
                    index++;
                }

                blocks.Add(new ParsedBlock(
                    BlockTypes.Code,
                    BlockPayloads.SerializeForWrite(new CodePayload { Code = code.ToString(), Language = language }),
                    code.ToString()
                ));
                continue;
            }

            if (line.Trim() == "---")
            {
                blocks.Add(new ParsedBlock(BlockTypes.Divider, BlockPayloads.SerializeForWrite(new DividerPayload()), string.Empty));
                index++;
                continue;
            }

            if (TryParseTodo(line, out var checkedState, out var todoText))
            {
                blocks.Add(CreateSpanBlock(BlockTypes.Todo, new TodoPayload { Checked = checkedState, Spans = ParseSpans(todoText) }));
                index++;
                continue;
            }

            if (line.StartsWith("> [!", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
            {
                var variant = line[4..^1].ToLowerInvariant();
                if (Enum.TryParse<PaletteColor>(variant, true, out var palette))
                {
                    index++;
                    // Only consume the next line when it is actually the callout body; otherwise
                    // it is ordinary content that the loop must still see.
                    var body = string.Empty;
                    if (index < lines.Length && lines[index].StartsWith("> ", StringComparison.Ordinal))
                    {
                        body = lines[index][2..];
                        index++;
                    }

                    blocks.Add(CreateSpanBlock(BlockTypes.Callout, new CalloutPayload { Variant = palette, Spans = ParseSpans(body) }));
                    continue;
                }
            }

            if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                blocks.Add(CreateSpanBlock(BlockTypes.Quote, new QuotePayload { Spans = ParseSpans(line[2..]) }));
                index++;
                continue;
            }

            var headingHashes = line.TakeWhile(character => character == '#').Count();
            if (headingHashes >= 3 && line.Length > headingHashes && line[headingHashes] == ' ')
            {
                // # and ## are the notebook and page titles, so block headings start at ### and
                // the exporter emits Level + 2 hashes; subtracting 2 round-trips the level, and
                // the clamp keeps foreign markdown inside the payload's 1..6 range.
                var level = Math.Clamp(headingHashes - 2, 1, 6);
                blocks.Add(CreateSpanBlock(BlockTypes.Heading, new HeadingPayload { Level = level, Spans = ParseSpans(line[(headingHashes + 1)..]) }));
                index++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                blocks.Add(CreateSpanBlock(BlockTypes.Paragraph, new ParagraphPayload { Spans = ParseSpans(line.Trim()) }));
            }

            index++;
        }

        if (currentPage is not null)
        {
            currentPage.Blocks.AddRange(blocks);
        }

        return new ParsedNotebook(title, pages);
    }

    private static ParsedBlock CreateSpanBlock<TPayload>(string type, TPayload payload)
        => new(type, BlockPayloads.SerializeForWrite(payload), ExtractText(payload));

    private static string ExtractText<TPayload>(TPayload payload)
        => payload switch
        {
            ParagraphPayload paragraph => paragraph.Spans.ToPlainText(),
            HeadingPayload heading => heading.Spans.ToPlainText(),
            TodoPayload todo => todo.Spans.ToPlainText(),
            QuotePayload quote => quote.Spans.ToPlainText(),
            CalloutPayload callout => callout.Spans.ToPlainText(),
            _ => string.Empty,
        };

    private static bool TryParseTodo(string line, out bool isChecked, out string text)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase))
        {
            isChecked = true;
            text = trimmed[6..];
            return true;
        }

        if (trimmed.StartsWith("- [ ] ", StringComparison.Ordinal))
        {
            isChecked = false;
            text = trimmed[6..];
            return true;
        }

        isChecked = false;
        text = string.Empty;
        return false;
    }

    private static Spans ParseSpans(string text)
    {
        var spans = new List<Span>();
        var index = 0;
        while (index < text.Length)
        {
            if (TryReadDelimited(text, ref index, "**", out var bold))
            {
                spans.Add(new Span(bold, [new BoldMark()]));
                continue;
            }

            if (TryReadDelimited(text, ref index, "`", out var code))
            {
                spans.Add(new Span(code, [new CodeMark()]));
                continue;
            }

            if (TryReadDelimited(text, ref index, "*", out var italic))
            {
                spans.Add(new Span(italic, [new ItalicMark()]));
                continue;
            }

            var next = NextMarker(text, index + 1);
            spans.Add(new Span(text[index..next], []));
            index = next;
        }

        return Spans.Create(spans);
    }

    private static bool TryReadDelimited(string text, ref int index, string delimiter, out string value)
    {
        value = string.Empty;
        if (!text.AsSpan(index).StartsWith(delimiter, StringComparison.Ordinal))
        {
            return false;
        }

        var end = text.IndexOf(delimiter, index + delimiter.Length, StringComparison.Ordinal);
        if (end <= index + delimiter.Length)
        {
            return false;
        }

        value = text[(index + delimiter.Length)..end];
        index = end + delimiter.Length;
        return true;
    }

    private static int NextMarker(string text, int index)
    {
        var positions = new[]
        {
            text.IndexOf("**", index, StringComparison.Ordinal),
            text.IndexOf('`', index),
            text.IndexOf('*', index),
        }.Where(position => position >= 0);
        return positions.DefaultIfEmpty(text.Length).Min();
    }
}

internal sealed record ParsedNotebook(string Title, IReadOnlyList<ParsedPage> Pages);

internal sealed record ParsedPage(string Title)
{
    public List<ParsedBlock> Blocks { get; } = [];
}

internal sealed record ParsedBlock(string Type, JsonElement Content, string PlainText);
