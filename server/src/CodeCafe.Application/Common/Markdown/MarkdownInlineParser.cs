using CodeCafe.Domain.Blocks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CodeCafe.Application.Common.Markdown;

// Walks a Markdig inline tree into our Spans value object. Hard line breaks become a soft
// break ('\n') inside the current segment — Spans allows them — so every Parse call yields
// exactly one segment; soft breaks collapse to a space, matching CommonMark's rendering.
internal static class MarkdownInlineParser
{
    public static IReadOnlyList<Spans> Parse(ContainerInline? container) => Parse(container?.FirstChild);

    public static IReadOnlyList<Spans> Parse(Inline? first)
    {
        var segments = new List<List<Span>> { new() };
        for (var inline = first; inline is not null; inline = inline.NextSibling)
        {
            Append(segments, inline, []);
        }

        return segments.Select(Spans.Create).ToList();
    }

    // Single-line contexts (table cells, callout marker lines): soft breaks collapse to a
    // space too, because these payloads render on one line.
    public static Spans ParseSingleLine(ContainerInline? container) => ParseSingleLine(container?.FirstChild);

    public static Spans ParseSingleLine(Inline? first)
    {
        var segments = Parse(first);
        var joined = new List<Span>();
        foreach (var segment in segments)
        {
            if (joined.Count > 0)
            {
                joined.Add(new Span(" ", []));
            }

            joined.AddRange(segment.Items);
        }

        return Spans.Create(joined.Select(span => new Span(span.Text.Replace('\n', ' '), span.Marks)));
    }

    private static void Append(List<List<Span>> segments, Inline inline, IReadOnlyList<Mark> marks)
    {
        switch (inline)
        {
            case Markdig.Extensions.TaskLists.TaskList:
            case HtmlInline:
                // Task markers surface as Todo blocks at the list level; raw HTML tags are stripped.
                break;
            // HtmlEntityInline derives from LiteralInline, so it must match first: its Content
            // is empty; the decoded entity lives in Transcoded.
            case HtmlEntityInline entity:
                segments[^1].Add(new Span(entity.Transcoded.ToString(), marks));
                break;
            case LiteralInline literal:
                segments[^1].Add(new Span(literal.Content.ToString(), marks));
                break;
            case CodeInline code:
                segments[^1].Add(new Span(code.Content, [.. marks, new CodeMark()]));
                break;
            case LineBreakInline { IsHard: true }:
                segments[^1].Add(new Span("\n", marks));
                break;
            case LineBreakInline:
                segments[^1].Add(new Span(" ", marks));
                break;
            case EmphasisInline emphasis:
                var mark = MarkFor(emphasis);
                AppendChildren(segments, emphasis, mark is null ? marks : [.. marks, mark]);
                break;
            case LinkInline { IsImage: true } image:
                // An inline image loses its visual shape; its alt text survives as plain text.
                AppendChildren(segments, image, marks);
                break;
            // AutolinkInline derives from LinkInline but has no children: the URL is the text.
            case AutolinkInline autolink:
                var url = autolink.Url ?? string.Empty;
                try
                {
                    segments[^1].Add(new Span(url, [.. marks, new LinkMark(url)]));
                }
                catch (ArgumentException)
                {
                    segments[^1].Add(new Span(url, marks));
                }

                break;
            case LinkInline link:
                AppendLink(segments, link, marks);
                break;
            case ContainerInline container:
                AppendChildren(segments, container, marks);
                break;
        }
    }

    private static void AppendChildren(List<List<Span>> segments, ContainerInline container, IReadOnlyList<Mark> marks)
    {
        for (var child = container.FirstChild; child is not null; child = child.NextSibling)
        {
            Append(segments, child, marks);
        }
    }

    private static void AppendLink(List<List<Span>> segments, LinkInline link, IReadOnlyList<Mark> marks)
    {
        // Build the children unmarked first: a link annotating whitespace-only text violates a
        // Spans invariant, and a relative href violates LinkMark's. Both degrade to plain text
        // rather than failing the import.
        var children = new List<List<Span>> { new() };
        AppendChildren(children, link, marks);

        LinkMark? linkMark = null;
        if (link.Url is { } url)
        {
            try
            {
                linkMark = new LinkMark(url);
            }
            catch (ArgumentException)
            {
                // Not an absolute http/https/mailto URL; the text still survives.
            }
        }

        var hasVisibleText = children.Any(segment => segment.Any(span => !string.IsNullOrWhiteSpace(span.Text)));
        if (linkMark is not null && hasVisibleText)
        {
            foreach (var segment in children)
            {
                for (var i = 0; i < segment.Count; i++)
                {
                    segment[i] = segment[i] with { Marks = [.. segment[i].Marks, linkMark] };
                }
            }
        }

        segments[^1].AddRange(children[0]);
        for (var i = 1; i < children.Count; i++)
        {
            segments.Add(children[i]);
        }
    }

    // EmphasisExtras gives us ~~strike~~, ~sub~, ^sup^, ==highlight== and ++inserted++ on top
    // of the standard *italic* and **bold**. Nesting falls out of the recursion.
    private static Mark? MarkFor(EmphasisInline emphasis)
        => (emphasis.DelimiterChar, emphasis.DelimiterCount) switch
        {
            ('*' or '_', 2) => new BoldMark(),
            ('*' or '_', 1) => new ItalicMark(),
            ('~', 2) => new StrikeMark(),
            ('~', 1) => new SubMark(),
            ('^', _) => new SupMark(),
            // No color information exists in markdown; warning (yellow) is the default highlight.
            ('=', 2) => new HighlightMark(PaletteColor.Warning),
            ('+', 2) => new UnderlineMark(),
            _ => null,
        };
}
