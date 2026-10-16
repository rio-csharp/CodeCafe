using System.Text.Json;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Domain.Tests.Blocks;

public sealed class MarkTests
{
    [Fact]
    public void Serialize_SimpleMark_WritesKindOnly()
    {
        Assert.Equal("""{"kind":"bold"}""", JsonSerializer.Serialize<Mark>(new BoldMark()));
    }

    [Fact]
    public void Serialize_Link_WritesLowercaseHref()
    {
        Assert.Equal(
            """{"kind":"link","href":"https://example.com"}""",
            JsonSerializer.Serialize<Mark>(new LinkMark("https://example.com")));
    }

    [Fact]
    public void Serialize_Color_WritesLowercasePaletteName()
    {
        Assert.Equal(
            """{"kind":"color","name":"danger"}""",
            JsonSerializer.Serialize<Mark>(new ColorMark(PaletteColor.Danger)));
    }

    [Fact]
    public void Serialize_Abbr_WritesTitle()
    {
        Assert.Equal(
            """{"kind":"abbr","title":"HyperText Markup Language"}""",
            JsonSerializer.Serialize<Mark>(new AbbrMark("HyperText Markup Language")));
    }

    [Fact]
    public void Deserialize_RoundTripsThroughDiscriminator()
    {
        var link = JsonSerializer.Deserialize<Mark>("""{"kind":"link","href":"mailto:a@b.c"}""");
        Assert.Equal(new LinkMark("mailto:a@b.c"), link);

        var highlight = JsonSerializer.Deserialize<Mark>("""{"kind":"highlight","name":"muted"}""");
        Assert.Equal(new HighlightMark(PaletteColor.Muted), highlight);

        var kbd = JsonSerializer.Deserialize<Mark>("""{"kind":"kbd"}""");
        Assert.Equal(new KbdMark(), kbd);
    }

    [Fact]
    public void Deserialize_UnknownKind_Throws() =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Mark>("""{"kind":"sparkles"}"""));

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/path")]
    [InlineData("")]
    public void LinkMark_DisallowedScheme_Throws(string href) =>
        Assert.Throws<ArgumentException>(() => new LinkMark(href));

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("mailto:a@b.c")]
    public void LinkMark_AllowedScheme_Passes(string href) =>
        Assert.Equal(href, new LinkMark(href).Href);

    [Fact]
    public void AbbrMark_EmptyTitle_Throws() =>
        Assert.Throws<ArgumentException>(() => new AbbrMark("  "));
}
