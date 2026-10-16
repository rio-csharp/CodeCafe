using System.Text.Json.Serialization;

namespace CodeCafe.Domain.Blocks;

// One discriminated union of inline marks, serialized as {"kind":"link","href":...} objects.
// Mark-local invariants live on the records here; span-level rules (mutually exclusive sup/sub,
// at most one link/color/highlight per span, canonical ordering) live in Spans.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BoldMark), "bold")]
[JsonDerivedType(typeof(ItalicMark), "italic")]
[JsonDerivedType(typeof(UnderlineMark), "underline")]
[JsonDerivedType(typeof(StrikeMark), "strike")]
[JsonDerivedType(typeof(CodeMark), "code")]
[JsonDerivedType(typeof(LinkMark), "link")]
[JsonDerivedType(typeof(ColorMark), "color")]
[JsonDerivedType(typeof(HighlightMark), "highlight")]
[JsonDerivedType(typeof(KbdMark), "kbd")]
[JsonDerivedType(typeof(SupMark), "sup")]
[JsonDerivedType(typeof(SubMark), "sub")]
[JsonDerivedType(typeof(AbbrMark), "abbr")]
public abstract record Mark;

public sealed record BoldMark : Mark;

public sealed record ItalicMark : Mark;

public sealed record UnderlineMark : Mark;

public sealed record StrikeMark : Mark;

public sealed record CodeMark : Mark;

public sealed record KbdMark : Mark;

public sealed record SupMark : Mark;

public sealed record SubMark : Mark;

public sealed record LinkMark : Mark
{
    public LinkMark(string href)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(href);
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeMailto))
        {
            throw new ArgumentException("Link href must be an absolute http, https, or mailto URL.", nameof(href));
        }

        Href = href;
    }

    [JsonPropertyName("href")]
    public string Href { get; init; }
}

public sealed record ColorMark([property: JsonPropertyName("name")] PaletteColor Name) : Mark;

public sealed record HighlightMark([property: JsonPropertyName("name")] PaletteColor Name) : Mark;

public sealed record AbbrMark : Mark
{
    public AbbrMark(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }

    [JsonPropertyName("title")]
    public string Title { get; init; }
}
