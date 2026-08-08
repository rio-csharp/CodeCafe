using CodeCafe.Application.Notes;
using CodeCafe.Modules.Notes.Infrastructure.Notes;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.Infrastructure.Tests;

public sealed class TipTapContentServiceTests
{
    [Fact]
    public void NormalizePageContent_PreservesLeadingHeadingNodes()
    {
        var service = new TipTapContentService();
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
        var service = new TipTapContentService();
        var content = new JsonArray();
        for (var i = 0; i < ITipTapContentService.MaxNodeCount; i++)
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
        Assert.Equal(ITipTapContentService.MaxNodeCount, result.Error.Details!["maxTipTapNodeCount"]);
        Assert.Equal(ITipTapContentService.MaxNodeCount + 1, result.Error.Details["actualTipTapNodeCount"]);
    }

    [Fact]
    public void NormalizePageContent_RemovesEmptyTextNodes()
    {
        var service = new TipTapContentService();
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
        var service = new TipTapContentService();
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
                            ["text"] = new string('a', ITipTapContentService.MaxTextLength + 1)
                        }
                    }
                }
            }
        });

        var result = service.NormalizePageContent(document);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_tiptap_document", result.Error!.Code);
        Assert.Equal(ITipTapContentService.MaxTextLength, result.Error.Details!["maxTipTapTextLength"]);
        Assert.Equal(ITipTapContentService.MaxTextLength + 1, result.Error.Details["actualTipTapTextLength"]);
    }

    [Fact]
    public void NormalizePageContent_PreservesHardBreaksInPlainText()
    {
        var service = new TipTapContentService();
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
        var service = new TipTapContentService();
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

    [Fact]
    public void NormalizePageContent_EmitsCanonicalJsonForUnchangedDocuments()
    {
        var service = new TipTapContentService();
        using var document = JsonDocument.Parse(
            "{ \"type\": \"doc\", \"content\": [ { \"type\": \"heading\", \"attrs\": { \"level\": 1.0 }, \"content\": [ { \"type\": \"text\", \"text\": \"T\\u00edtle \\u201Cq\\u201D\" } ] } ] }");

        var result = service.NormalizePageContent(document.RootElement);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "{\"type\":\"doc\",\"content\":[{\"type\":\"heading\",\"attrs\":{\"level\":1.0},\"content\":[{\"type\":\"text\",\"text\":\"Títle “q”\"}]}]}",
            result.Value!.ContentJson);
        Assert.Equal("Títle “q”", result.Value.PlainTextContent);
    }

    [Fact]
    public void NormalizePageContent_AddsMissingRootContent()
    {
        var service = new TipTapContentService();
        using var document = JsonDocument.Parse("""{ "type": "doc" }""");

        var result = service.NormalizePageContent(document.RootElement);

        Assert.True(result.Succeeded);
        Assert.Equal("""{"type":"doc","content":[]}""", result.Value!.ContentJson);
        Assert.Null(result.Value.PlainTextContent);
    }

    [Fact]
    public void NormalizePageContent_NormalizesNullRootContent()
    {
        var service = new TipTapContentService();
        using var document = JsonDocument.Parse("""{ "type": "doc", "content": null }""");

        var result = service.NormalizePageContent(document.RootElement);

        Assert.True(result.Succeeded);
        Assert.Equal("""{"type":"doc","content":[]}""", result.Value!.ContentJson);
        Assert.Null(result.Value.PlainTextContent);
    }

    [Fact]
    public void NormalizePageContent_SkipsRemovedEmptyTextNodesWhenSeparatingPlainText()
    {
        var service = new TipTapContentService();
        using var document = JsonDocument.Parse("""
            {
              "type": "doc",
              "content": [
                { "type": "paragraph", "content": [{ "type": "text", "text": "One" }] },
                { "type": "text", "text": "" },
                { "type": "paragraph", "content": [{ "type": "text", "text": "Two" }] }
              ]
            }
            """);

        var result = service.NormalizePageContent(document.RootElement);

        Assert.True(result.Succeeded);
        Assert.Equal($"One{Environment.NewLine}Two", result.Value!.PlainTextContent);
        using var normalized = JsonDocument.Parse(result.Value.ContentJson!);
        Assert.Equal(2, normalized.RootElement.GetProperty("content").GetArrayLength());
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("//evil.example/phish")]
    [InlineData("  javascript:alert(1)")]
    public void NormalizePageContent_RemovesLinkMarksWithUnsafeUrls(string href)
    {
        var service = new TipTapContentService();
        var document = CreateDocumentWithLinkedText(href);

        var result = service.NormalizePageContent(document);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain("\"type\":\"link\"", result.Value!.ContentJson, StringComparison.Ordinal);
        Assert.Equal("click me", result.Value.PlainTextContent);
        using var normalized = JsonDocument.Parse(result.Value.ContentJson!);
        var textNode = normalized.RootElement.GetProperty("content")[0].GetProperty("content")[0];
        Assert.Equal("click me", textNode.GetProperty("text").GetString());
        Assert.False(textNode.TryGetProperty("marks", out var marks) && marks.GetArrayLength() > 0);
    }

    [Theory]
    [InlineData("https://example.com/page")]
    [InlineData("http://example.com")]
    [InlineData("mailto:user@example.com")]
    [InlineData("tel:+1234567890")]
    [InlineData("/docs/getting-started")]
    [InlineData("/")]
    public void NormalizePageContent_KeepsLinkMarksWithAllowedUrls(string href)
    {
        var service = new TipTapContentService();
        var document = CreateDocumentWithLinkedText(href);

        var result = service.NormalizePageContent(document);

        Assert.True(result.Succeeded);
        using var normalized = JsonDocument.Parse(result.Value!.ContentJson!);
        var textNode = normalized.RootElement.GetProperty("content")[0].GetProperty("content")[0];
        var linkMark = textNode.GetProperty("marks")[0];
        Assert.Equal("link", linkMark.GetProperty("type").GetString());
        Assert.Equal(href, linkMark.GetProperty("attrs").GetProperty("href").GetString());
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("//evil.example/tracker.png")]
    [InlineData("mailto:user@example.com")]
    public void NormalizePageContent_RemovesSrcFromImagesWithUnsafeUrls(string src)
    {
        var service = new TipTapContentService();
        var document = JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "doc",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "image",
                    ["attrs"] = new JsonObject
                    {
                        ["src"] = src,
                        ["alt"] = "pic"
                    }
                }
            }
        });

        var result = service.NormalizePageContent(document);

        Assert.True(result.Succeeded);
        using var normalized = JsonDocument.Parse(result.Value!.ContentJson!);
        var image = normalized.RootElement.GetProperty("content")[0];
        Assert.Equal("image", image.GetProperty("type").GetString());
        Assert.False(image.GetProperty("attrs").TryGetProperty("src", out _));
        Assert.Equal("pic", image.GetProperty("attrs").GetProperty("alt").GetString());
        Assert.Equal("[Image]", result.Value.PlainTextContent);
    }

    [Theory]
    [InlineData("https://cdn.example.com/image.png")]
    [InlineData("http://example.com/a.gif")]
    [InlineData("/uploads/image.png")]
    public void NormalizePageContent_KeepsImageSrcForAllowedUrls(string src)
    {
        var service = new TipTapContentService();
        var document = JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "doc",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "image",
                    ["attrs"] = new JsonObject
                    {
                        ["src"] = src,
                        ["alt"] = "pic"
                    }
                }
            }
        });

        var result = service.NormalizePageContent(document);

        Assert.True(result.Succeeded);
        using var normalized = JsonDocument.Parse(result.Value!.ContentJson!);
        var image = normalized.RootElement.GetProperty("content")[0];
        Assert.Equal(src, image.GetProperty("attrs").GetProperty("src").GetString());
    }

    [Fact]
    public void NormalizePageContent_RemovesSrcFromYoutubeEmbedsWithUnsafeUrls()
    {
        var service = new TipTapContentService();
        var document = JsonSerializer.SerializeToElement(new JsonObject
        {
            ["type"] = "doc",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "youtube",
                    ["attrs"] = new JsonObject { ["src"] = "javascript:alert(1)" }
                }
            }
        });

        var result = service.NormalizePageContent(document);

        Assert.True(result.Succeeded);
        using var normalized = JsonDocument.Parse(result.Value!.ContentJson!);
        var youtube = normalized.RootElement.GetProperty("content")[0];
        Assert.Equal("youtube", youtube.GetProperty("type").GetString());
        Assert.False(youtube.GetProperty("attrs").TryGetProperty("src", out _));
        Assert.Equal("[Video]", result.Value.PlainTextContent);
    }

    private static JsonElement CreateDocumentWithLinkedText(string href)
    {
        return JsonSerializer.SerializeToElement(new JsonObject
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
                            ["text"] = "click me",
                            ["marks"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "link",
                                    ["attrs"] = new JsonObject { ["href"] = href }
                                }
                            }
                        }
                    }
                }
            }
        });
    }
}
