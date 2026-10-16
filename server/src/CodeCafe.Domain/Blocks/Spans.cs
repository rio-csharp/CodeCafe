using System.Text.Json.Serialization;

namespace CodeCafe.Domain.Blocks;

// One span of rich text: visible text plus the marks applying to all of it. Marks are compared
// by value, so equality survives canonical reordering.
public sealed record Span
{
    public Span(string text, IReadOnlyList<Mark>? marks)
    {
        Text = text;
        Marks = marks ?? [];
    }

    [JsonPropertyName("text")]
    public string Text { get; init; }

    [JsonPropertyName("marks")]
    public IReadOnlyList<Mark> Marks { get; init; }

    public bool Equals(Span? other)
        => other is not null && Text == other.Text && Marks.SequenceEqual(other.Marks);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Text);
        foreach (var mark in Marks)
        {
            hash.Add(mark);
        }

        return hash.ToHashCode();
    }
}

// Rich text as a flat span list. This value object owns every invariant — canonicalization,
// validation, plain-text projection — so handlers and clients never reimplement them. Rule
// violations are caller bugs, signalled with ArgumentException like the rest of the domain.
public sealed class Spans : IEquatable<Spans>
{
    // Empty text content has exactly one legal shape: no spans at all.
    public static readonly Spans Empty = new([]);

    private Spans(IReadOnlyList<Span> items) => Items = items;

    public IReadOnlyList<Span> Items { get; }

    // Canonicalizes while validating: empty-text spans drop out, adjacent spans with identical
    // mark sets merge, and each span's marks land in canonical order. Whitespace is never
    // trimmed — leading and trailing spaces are meaningful text.
    public static Spans Create(IEnumerable<Span> spans)
    {
        ArgumentNullException.ThrowIfNull(spans);

        var canonical = new List<Span>();
        foreach (var span in spans)
        {
            ArgumentNullException.ThrowIfNull(span);
            var text = span.Text ?? throw new ArgumentException("Span text must not be null.", nameof(spans));
            // Soft line breaks (Shift+Enter in the editor) are legal text; line
            // endings normalize to LF so the canonical form is stable.
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');

            var marks = CanonicalizeMarks(span.Marks);
            if (text.Length == 0)
            {
                continue;
            }

            if (canonical.Count > 0 && canonical[^1].Marks.SequenceEqual(marks))
            {
                canonical[^1] = canonical[^1] with { Text = canonical[^1].Text + text };
            }
            else
            {
                canonical.Add(new Span(text, marks));
            }
        }

        ValidateLinkRuns(canonical);
        return canonical.Count == 0 ? Empty : new Spans(canonical);
    }

    public string ToPlainText() => string.Concat(Items.Select(span => span.Text));

    public bool Equals(Spans? other) => other is not null && Items.SequenceEqual(other.Items);

    public override bool Equals(object? obj) => Equals(obj as Spans);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var span in Items)
        {
            hash.Add(span);
        }

        return hash.ToHashCode();
    }

    private static IReadOnlyList<Mark> CanonicalizeMarks(IReadOnlyList<Mark> marks)
    {
        if (marks.Count(mark => mark is LinkMark) > 1)
        {
            throw new ArgumentException("A span carries at most one link mark.", nameof(marks));
        }

        if (marks.Count(mark => mark is ColorMark) > 1)
        {
            throw new ArgumentException("A span carries at most one color mark.", nameof(marks));
        }

        if (marks.Count(mark => mark is HighlightMark) > 1)
        {
            throw new ArgumentException("A span carries at most one highlight mark.", nameof(marks));
        }

        if (marks.Any(mark => mark is SupMark) && marks.Any(mark => mark is SubMark))
        {
            throw new ArgumentException("A span cannot be both superscript and subscript.", nameof(marks));
        }

        // Canonical order follows the render priority (link > code > sup/sub > styles), so one
        // logical mark set has exactly one physical shape and merging compares by sequence.
        return marks
            .Distinct()
            .OrderBy(MarkOrder)
            .ThenBy(MarkParameter, StringComparer.Ordinal)
            .ToArray();
    }

    // A link annotating only whitespace is invisible and unclickable. The check applies to a
    // whole contiguous run of spans sharing the same link, not per span: a link split by style
    // boundaries is legal as long as the run's aggregate visible text is non-whitespace.
    private static void ValidateLinkRuns(IReadOnlyList<Span> spans)
    {
        string? currentHref = null;
        var runHasVisibleText = false;

        void CloseRun()
        {
            if (currentHref is not null && !runHasVisibleText)
            {
                throw new ArgumentException("A link must annotate non-whitespace text.", nameof(spans));
            }
        }

        foreach (var span in spans)
        {
            var href = span.Marks.OfType<LinkMark>().FirstOrDefault()?.Href;
            if (href != currentHref)
            {
                CloseRun();
                currentHref = href;
                runHasVisibleText = false;
            }

            if (href is not null && !string.IsNullOrWhiteSpace(span.Text))
            {
                runHasVisibleText = true;
            }
        }

        CloseRun();
    }

    private static int MarkOrder(Mark mark) => mark switch
    {
        LinkMark => 0,
        CodeMark => 1,
        SupMark => 2,
        SubMark => 3,
        BoldMark => 4,
        ItalicMark => 5,
        UnderlineMark => 6,
        StrikeMark => 7,
        KbdMark => 8,
        AbbrMark => 9,
        ColorMark => 10,
        HighlightMark => 11,
        _ => throw new ArgumentOutOfRangeException(nameof(mark))
    };

    private static string MarkParameter(Mark mark) => mark switch
    {
        LinkMark link => link.Href,
        ColorMark color => color.Name.ToString(),
        HighlightMark highlight => highlight.Name.ToString(),
        AbbrMark abbr => abbr.Title,
        _ => string.Empty
    };
}
