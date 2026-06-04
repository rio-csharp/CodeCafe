using CodeCafe.Infrastructure.Notes;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.Infrastructure.Tests;

public sealed class TipTapContentServiceTests
{
    [Fact]
    public void NormalizePageContent_PreservesLeadingHeadingNodes()
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

        var result = service.NormalizePageContent(document.RootElement);

        Assert.True(result.Succeeded);
        using var normalized = JsonDocument.Parse(result.Value!.ContentJson!);
        var firstNode = normalized.RootElement.GetProperty("content")[0];
        Assert.Equal("heading", firstNode.GetProperty("type").GetString());
        Assert.Equal(1, firstNode.GetProperty("attrs").GetProperty("level").GetInt32());
        Assert.Equal("async-await intro", firstNode.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Fact]
    public void NormalizePageContent_ReturnsDetailsWhenNodeLimitIsExceeded()
    {
        var service = new TipTapContentService(new TipTapPlainTextExtractor());
        var content = new JsonArray();
        for (var i = 0; i < TipTapContentService.MaxNodeCount; i++)
        {
            content.Add(new JsonObject
            {
                ["type"] = "paragraph"
            });
        }

        var root = new JsonObject
        {
            ["type"] = "doc",
            ["content"] = content
        };
        var document = JsonSerializer.SerializeToElement(root);

        var result = service.NormalizePageContent(document);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_tiptap_document", result.Error!.Code);
        Assert.Equal("contentJson", result.Error.Field);
        Assert.Equal(TipTapContentService.MaxNodeCount, result.Error.Details!["maxTipTapNodeCount"]);
        Assert.Equal(TipTapContentService.MaxNodeCount + 1, result.Error.Details["actualTipTapNodeCount"]);
    }

    [Fact]
    public void NormalizePageContent_RemovesEmptyTextNodes()
    {
        var service = new TipTapContentService(new TipTapPlainTextExtractor());
        using var document = JsonDocument.Parse("""
            {
              "type": "doc",
              "content": [
                {
                  "type": "paragraph",
                  "content": [
                    { "type": "text", "text": "" },
                    { "type": "text", "text": "Hello" }
                  ]
                }
              ]
            }
            """);

        var result = service.NormalizePageContent(document.RootElement);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain("\"text\":\"\"", result.Value!.ContentJson, StringComparison.Ordinal);
        using var normalized = JsonDocument.Parse(result.Value.ContentJson!);
        var paragraphContent = normalized.RootElement.GetProperty("content")[0].GetProperty("content");
        Assert.Single(paragraphContent.EnumerateArray());
        Assert.Equal("Hello", paragraphContent[0].GetProperty("text").GetString());
    }

    [Fact]
    public void NormalizePageContent_ReturnsDetailsWhenTextLimitIsExceeded()
    {
        var service = new TipTapContentService(new TipTapPlainTextExtractor());
        var document = JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "doc",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = new string('a', TipTapContentService.MaxTextLength + 1)
                        }
                    }
                }
            }
        });

        var result = service.NormalizePageContent(document);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_tiptap_document", result.Error!.Code);
        Assert.Equal(TipTapContentService.MaxTextLength, result.Error.Details!["maxTipTapTextLength"]);
        Assert.Equal(TipTapContentService.MaxTextLength + 1, result.Error.Details["actualTipTapTextLength"]);
    }

    [Fact]
    public void NormalizePageContent_PreservesHardBreaksInPlainText()
    {
        var service = new TipTapContentService(new TipTapPlainTextExtractor());
        using var document = JsonDocument.Parse("""
            {
              "type": "doc",
              "content": [
                {
                  "type": "paragraph",
                  "content": [
                    { "type": "text", "text": "Line one" },
                    { "type": "hardBreak" },
                    { "type": "text", "text": "Line two" }
                  ]
                }
              ]
            }
            """);

        var result = service.NormalizePageContent(document.RootElement);

        Assert.True(result.Succeeded);
        var plainText = result.Value!.PlainTextContent!;
        Assert.Contains("Line one", plainText, StringComparison.Ordinal);
        Assert.Contains("Line two", plainText, StringComparison.Ordinal);
        Assert.Contains(Environment.NewLine, plainText, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizePageContent_PreservesCurlyDoubleQuotesInStoredJsonAndPlainText()
    {
        var service = new TipTapContentService(new TipTapPlainTextExtractor());
        var document = JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "doc",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = "Quote: \u201Cvalue\u201D"
                        }
                    }
                }
            }
        });

        var result = service.NormalizePageContent(document);

        Assert.True(result.Succeeded);
        Assert.Equal("Quote: \u201Cvalue\u201D", result.Value!.PlainTextContent);
        Assert.Contains("\u201C", result.Value.ContentJson, StringComparison.Ordinal);
        Assert.Contains("\u201D", result.Value.ContentJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u201C", result.Value.ContentJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u201D", result.Value.ContentJson, StringComparison.OrdinalIgnoreCase);
    }
}
