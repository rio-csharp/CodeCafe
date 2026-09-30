using System.Text.Json;

using CodeCafe.Application.Blocks.Shared;

namespace CodeCafe.Application.Tests.Blocks;

public sealed class BlockPayloadsTests
{
    public static TheoryData<string, string, string> ValidPayloadsWithPlainText => new()
    {
        { BlockTypes.Paragraph, """{"spans":[{"text":"Hello","marks":[]}]}""", "Hello" },
        { BlockTypes.Heading, """{"level":2,"spans":[{"text":"Title","marks":[]}]}""", "Title" },
        { BlockTypes.Todo, """{"checked":true,"spans":[{"text":"Buy milk","marks":[]}]}""", "Buy milk" },
        { BlockTypes.Code, """{"code":"var x = 1;","language":"csharp"}""", "var x = 1;" },
        { BlockTypes.Quote, """{"spans":[{"text":"Wisdom","marks":[]}]}""", "Wisdom" },
        { BlockTypes.Callout, """{"variant":"warning","spans":[{"text":"Careful","marks":[]}]}""", "Careful" },
        { BlockTypes.Divider, "{}", "" },
        { BlockTypes.Image, """{"url":"https://example.com/cat.png","alt":"A cat","isDecorative":false}""", "A cat" },
        { BlockTypes.Audio, """{"url":"https://example.com/pod.mp3","mimeType":"audio/mpeg","duration":183}""", "" },
    };

    [Theory]
    [MemberData(nameof(ValidPayloadsWithPlainText))]
    public void ValidateAndNormalize_RoundTripsValidPayloads(string type, string json, string expectedPlainText)
    {
        var result = Validate(type, json);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(expectedPlainText, result.Value!.PlainText);

        // Validating the canonical output must succeed and reproduce it byte for byte.
        var reparsed = Validate(type, result.Value.CanonicalJson);
        Assert.True(reparsed.IsSuccess, reparsed.Error?.Message);
        Assert.Equal(result.Value.CanonicalJson, reparsed.Value!.CanonicalJson);
        Assert.Equal(result.Value.PlainText, reparsed.Value.PlainText);
    }

    [Fact]
    public void ValidateAndNormalize_MergesAdjacentSameMarkSpans_InCanonicalJson()
    {
        var result = Validate(
            BlockTypes.Paragraph,
            """{"spans":[{"text":"Hello ","marks":[{"kind":"bold"}]},{"text":"world","marks":[{"kind":"bold"}]}]}"""
        );

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("""{"spans":[{"text":"Hello world","marks":[{"kind":"bold"}]}]}""", result.Value!.CanonicalJson);
        Assert.Equal("Hello world", result.Value.PlainText);
    }

    [Fact]
    public void ValidateAndNormalize_LowercasesCodeLanguage()
    {
        var result = Validate(BlockTypes.Code, """{"code":"fn main() {}","language":"  Rust "}""");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("""{"code":"fn main() {}","language":"rust"}""", result.Value!.CanonicalJson);
    }

    [Fact]
    public void ValidateAndNormalize_FallsBackToCaption_WhenDecorativeImageHasNoAlt()
    {
        var result = Validate(
            BlockTypes.Image,
            """{"url":"https://example.com/bg.png","isDecorative":true,"caption":"Blue gradient"}"""
        );

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("Blue gradient", result.Value!.PlainText);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void ValidateAndNormalize_RejectsHeadingLevelOutOfRange(int level)
    {
        var result = Validate(BlockTypes.Heading, $$"""{"level":{{level}},"spans":[]}""");

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsImageWithoutAlt_WhenNotDecorative()
    {
        var result = Validate(BlockTypes.Image, """{"url":"https://example.com/cat.png","isDecorative":false}""");

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsUnknownType()
    {
        var result = Validate("sparkles", "{}");

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.UnsupportedBlockType.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsUnknownJsonProperty()
    {
        var result = Validate(BlockTypes.Paragraph, """{"spans":[],"surprise":true}""");

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsSpanTextWithNewline()
    {
        var result = Validate(BlockTypes.Paragraph, """{"spans":[{"text":"one\ntwo","marks":[]}]}""");

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsUnknownMarkKind()
    {
        var result = Validate(BlockTypes.Paragraph, """{"spans":[{"text":"hi","marks":[{"kind":"sparkles"}]}]}""");

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload.Code, result.Error!.Code);
    }

    [Fact]
    public void ValidateAndNormalize_RejectsNullSpans()
    {
        var result = Validate(BlockTypes.Paragraph, """{"spans":null}""");

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload.Code, result.Error!.Code);
    }

    private static CodeCafe.Application.Common.Result<NormalizedBlockPayload> Validate(string type, string json)
    {
        using var document = JsonDocument.Parse(json);
        return BlockPayloads.ValidateAndNormalize(type, document.RootElement);
    }
}
