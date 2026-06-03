using CodeCafe.Infrastructure.Notes;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Tests;

public sealed class TipTapContentServiceTests
{
    [Fact]
    public void NormalizePageContent_StripsOnlyLeadingH1ThatDuplicatesThePageTitle()
    {
        var service = new TipTapContentService(new TipTapPlainTextExtractor());
        using var document = JsonDocument.Parse("""
            {
              "type": "doc",
              "content": [
                {
                  "type": "heading",
                  "attrs": { "level": 1 },
                  "content": [{ "type": "text", "text": "async-await intro" }]
                },
                {
                  "type": "paragraph",
                  "content": [{ "type": "text", "text": "First paragraph." }]
                }
              ]
            }
            """);

        var result = service.NormalizePageContent(document.RootElement, "async-await intro");

        Assert.True(result.Succeeded);
        using var normalized = JsonDocument.Parse(result.Value!.ContentJson!);
        var firstNode = normalized.RootElement.GetProperty("content")[0];
        Assert.Equal("paragraph", firstNode.GetProperty("type").GetString());
    }

    [Fact]
    public void NormalizePageContent_PreservesH1WhenItDoesNotDuplicateThePageTitle()
    {
        var service = new TipTapContentService(new TipTapPlainTextExtractor());
        using var document = JsonDocument.Parse("""
            {
              "type": "doc",
              "content": [
                {
                  "type": "heading",
                  "attrs": { "level": 1 },
                  "content": [{ "type": "text", "text": "Body heading" }]
                },
                {
                  "type": "paragraph",
                  "content": [{ "type": "text", "text": "First paragraph." }]
                }
              ]
            }
            """);

        var result = service.NormalizePageContent(document.RootElement, "Page title");

        Assert.True(result.Succeeded);
        using var normalized = JsonDocument.Parse(result.Value!.ContentJson!);
        var firstNode = normalized.RootElement.GetProperty("content")[0];
        Assert.Equal("heading", firstNode.GetProperty("type").GetString());
        Assert.Equal(1, firstNode.GetProperty("attrs").GetProperty("level").GetInt32());
    }
}
