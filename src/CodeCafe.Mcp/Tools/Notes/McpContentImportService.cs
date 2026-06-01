using CodeCafe.Mcp.Configuration;
using CodeCafe.Application.Notes;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.Mcp.Tools.Notes;

public interface IMcpContentImportService
{
    NotesResult<JsonElement?> ResolveOptionalPageContent(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage);

    NotesResult<JsonElement> ResolveRequiredPageContent(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage);

    NotesResult<JsonElement> ResolveRequiredBlocks(
        Guid actorId,
        JsonElement? inlineBlocks,
        string? blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage);

    NotesResult EnforcePageContentSize(JsonElement contentJson, string errorCode);

    void DeleteUpload(Guid actorId, string? uploadId);
}

public interface IMcpMarkdownImporter
{
    JsonElement ConvertMarkdownToDocument(string markdown);

    JsonElement ConvertMarkdownToBlocks(string markdown);
}

public sealed class McpContentImportService(
    IMcpUploadStore uploadStore,
    IMcpMarkdownImporter markdownImporter,
    IOptions<McpOptions> mcpOptionsAccessor) : IMcpContentImportService
{
    public NotesResult<JsonElement?> ResolveOptionalPageContent(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage)
    {
        var hasInlineContent = inlineContentJson is not null
            && inlineContentJson.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var hasUpload = !string.IsNullOrWhiteSpace(contentUploadId);

        if (!hasInlineContent && !hasUpload)
        {
            return NotesResult<JsonElement?>.Success(null);
        }

        if (hasInlineContent && hasUpload)
        {
            return NotesResult<JsonElement?>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "Provide either inline contentJson or contentUploadId, but not both.");
        }

        var resolved = hasUpload
            ? ResolveUploadAsPageContent(actorId, contentUploadId!, contentFormat, errorCode, invalidMessage)
            : ResolveInlineJson(inlineContentJson!.Value, errorCode, invalidMessage);

        if (!resolved.Succeeded)
        {
            return NotesResult<JsonElement?>.Failure(resolved.Error!.Kind, resolved.Error.Code, resolved.Error.Message);
        }

        return NotesResult<JsonElement?>.Success(resolved.Value);
    }

    public NotesResult<JsonElement> ResolveRequiredPageContent(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage)
    {
        var hasInlineContent = inlineContentJson is not null
            && inlineContentJson.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var hasUpload = !string.IsNullOrWhiteSpace(contentUploadId);

        if (hasInlineContent && hasUpload)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "Provide either inline contentJson or contentUploadId, but not both.");
        }

        if (hasUpload)
        {
            return ResolveUploadAsPageContent(actorId, contentUploadId!, contentFormat, errorCode, invalidMessage);
        }

        if (!hasInlineContent)
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage);
        }

        return ResolveInlineJson(inlineContentJson!.Value, errorCode, invalidMessage);
    }

    public NotesResult<JsonElement> ResolveRequiredBlocks(
        Guid actorId,
        JsonElement? inlineBlocks,
        string? blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage)
    {
        var hasInlineBlocks = inlineBlocks is not null
            && inlineBlocks.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var hasUpload = !string.IsNullOrWhiteSpace(blocksUploadId);

        if (hasInlineBlocks && hasUpload)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "Provide either inline blocks or blocksUploadId, but not both.");
        }

        if (hasUpload)
        {
            return ResolveUploadAsBlocks(actorId, blocksUploadId!, blocksFormat, errorCode, invalidMessage);
        }

        if (!hasInlineBlocks)
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage);
        }

        var result = ResolveInlineJson(inlineBlocks!.Value, errorCode, invalidMessage);
        if (!result.Succeeded)
        {
            return result;
        }

        return result.Value.ValueKind == JsonValueKind.Array
            ? result
            : NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, "Blocks must be a JSON array.");
    }

    public NotesResult EnforcePageContentSize(JsonElement contentJson, string errorCode)
    {
        var currentBytes = Encoding.UTF8.GetByteCount(contentJson.GetRawText());
        var maxBytes = mcpOptionsAccessor.Value.MaxPageContentBytes;
        return currentBytes <= maxBytes
            ? NotesResult.Success()
            : NotesResult.Failure(
                NotesFailureKind.Validation,
                errorCode,
                $"Page content exceeds the limit of {maxBytes} bytes (received {currentBytes} bytes).");
    }

    public void DeleteUpload(Guid actorId, string? uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            return;
        }

        uploadStore.Delete(actorId, uploadId);
    }

    private NotesResult<JsonElement> ResolveUploadAsPageContent(
        Guid actorId,
        string contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage)
    {
        var uploadResult = uploadStore.Get(actorId, contentUploadId);
        if (!uploadResult.Succeeded)
        {
            return ToNotesResult(uploadResult.Error!, errorCode);
        }

        var session = uploadResult.Value!;
        var format = NormalizePageFormat(contentFormat, session.MediaType, session.FileName);
        return format switch
        {
            "tiptap_json" => ParseJsonText(session.ContentText, errorCode, invalidMessage, allowArray: false),
            "markdown" => ConvertMarkdownDocument(session.ContentText, errorCode),
            _ => NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "contentFormat must be tiptap_json or markdown.")
        };
    }

    private NotesResult<JsonElement> ResolveUploadAsBlocks(
        Guid actorId,
        string blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage)
    {
        var uploadResult = uploadStore.Get(actorId, blocksUploadId);
        if (!uploadResult.Succeeded)
        {
            return ToNotesResult(uploadResult.Error!, errorCode);
        }

        var session = uploadResult.Value!;
        var format = NormalizeBlocksFormat(blocksFormat, session.MediaType, session.FileName);
        return format switch
        {
            "tiptap_blocks_json" => ParseJsonText(session.ContentText, errorCode, invalidMessage, allowArray: true, requireArray: true),
            "markdown" => ConvertMarkdownBlocks(session.ContentText, errorCode),
            _ => NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "blocksFormat must be tiptap_blocks_json or markdown.")
        };
    }

    private NotesResult<JsonElement> ResolveInlineJson(
        JsonElement json,
        string errorCode,
        string invalidMessage)
    {
        if (json.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage);
        }

        var rawText = json.ValueKind == JsonValueKind.String
            ? json.GetString()
            : json.GetRawText();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage);
        }

        var maxBytes = mcpOptionsAccessor.Value.MaxInlineContentBytes;
        var currentBytes = Encoding.UTF8.GetByteCount(rawText);
        if (currentBytes > maxBytes)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                "content_too_large",
                $"Inline content exceeds the limit of {maxBytes} bytes (received {currentBytes} bytes).");
        }

        if (json.ValueKind == JsonValueKind.String)
        {
            return ParseJsonText(rawText, errorCode, invalidMessage, allowArray: true);
        }

        return NotesResult<JsonElement>.Success(json);
    }

    private NotesResult<JsonElement> ConvertMarkdownDocument(string markdown, string errorCode)
    {
        try
        {
            return NotesResult<JsonElement>.Success(markdownImporter.ConvertMarkdownToDocument(markdown));
        }
        catch (Exception ex)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                $"Markdown could not be converted: {ex.Message}");
        }
    }

    private NotesResult<JsonElement> ConvertMarkdownBlocks(string markdown, string errorCode)
    {
        try
        {
            return NotesResult<JsonElement>.Success(markdownImporter.ConvertMarkdownToBlocks(markdown));
        }
        catch (Exception ex)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                $"Markdown could not be converted: {ex.Message}");
        }
    }

    private static NotesResult<JsonElement> ParseJsonText(
        string rawText,
        string errorCode,
        string invalidMessage,
        bool allowArray,
        bool requireArray = false)
    {
        try
        {
            using var document = JsonDocument.Parse(rawText);
            var value = document.RootElement.Clone();
            if (!allowArray && value.ValueKind == JsonValueKind.Array)
            {
                return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage);
            }

            if (requireArray && value.ValueKind != JsonValueKind.Array)
            {
                return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, "Blocks must be a JSON array.");
            }

            return NotesResult<JsonElement>.Success(value);
        }
        catch (JsonException)
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage);
        }
    }

    private static NotesResult<JsonElement> ToNotesResult(NotesUploadError error, string fallbackCode)
    {
        return NotesResult<JsonElement>.Failure(
            NotesFailureKind.Validation,
            string.IsNullOrWhiteSpace(error.Code) ? fallbackCode : error.Code,
            error.Message);
    }

    private static string NormalizePageFormat(string? requestedFormat, string mediaType, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(requestedFormat))
        {
            return requestedFormat.Trim().ToLowerInvariant();
        }

        if (IsMarkdown(mediaType, fileName))
        {
            return "markdown";
        }

        return "tiptap_json";
    }

    private static string NormalizeBlocksFormat(string? requestedFormat, string mediaType, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(requestedFormat))
        {
            return requestedFormat.Trim().ToLowerInvariant();
        }

        if (IsMarkdown(mediaType, fileName))
        {
            return "markdown";
        }

        return "tiptap_blocks_json";
    }

    private static bool IsMarkdown(string mediaType, string? fileName)
    {
        if (mediaType.Contains("markdown", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(fileName)
            && fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class MarkdigMcpMarkdownImporter : IMcpMarkdownImporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public JsonElement ConvertMarkdownToDocument(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        var content = ConvertBlocks(document);
        var root = new JsonObject
        {
            ["type"] = "doc",
            ["content"] = content
        };

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public JsonElement ConvertMarkdownToBlocks(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        return JsonSerializer.SerializeToElement(ConvertBlocks(document), SerializerOptions);
    }

    private static JsonArray ConvertBlocks(ContainerBlock container)
    {
        var blocks = new JsonArray();
        foreach (var block in container)
        {
            foreach (var converted in ConvertBlock(block))
            {
                blocks.Add(converted);
            }
        }

        return blocks;
    }

    private static IEnumerable<JsonNode> ConvertBlock(Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                yield return new JsonObject
                {
                    ["type"] = "heading",
                    ["attrs"] = new JsonObject { ["level"] = Math.Clamp(heading.Level, 1, 4) },
                    ["content"] = ConvertInlineContainer(heading.Inline)
                };
                yield break;

            case ParagraphBlock paragraph:
                yield return new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = ConvertInlineContainer(paragraph.Inline)
                };
                yield break;

            case QuoteBlock quote:
                yield return new JsonObject
                {
                    ["type"] = "blockquote",
                    ["content"] = ConvertBlocks(quote)
                };
                yield break;

            case FencedCodeBlock fencedCode:
                yield return CreateCodeBlock(fencedCode.Info, ExtractBlockText(fencedCode));
                yield break;

            case CodeBlock codeBlock:
                yield return CreateCodeBlock(null, ExtractBlockText(codeBlock));
                yield break;

            case ThematicBreakBlock:
                yield return new JsonObject { ["type"] = "horizontalRule" };
                yield break;

            case ListBlock list:
                yield return ConvertList(list);
                yield break;

            case Table table:
                yield return ConvertTable(table);
                yield break;

            case HtmlBlock html:
                yield return CreateParagraphFromText(ExtractBlockText(html));
                yield break;

            default:
                var text = ExtractBlockText(block);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return CreateParagraphFromText(text);
                }
                yield break;
        }
    }

    private static JsonObject ConvertList(ListBlock list)
    {
        var listType = list.IsOrdered ? "orderedList" : "bulletList";
        var items = new JsonArray();
        foreach (var child in list.OfType<ListItemBlock>())
        {
            items.Add(new JsonObject
            {
                ["type"] = "listItem",
                ["content"] = ConvertBlocks(child)
            });
        }

        var node = new JsonObject
        {
            ["type"] = listType,
            ["content"] = items
        };

        return node;
    }

    private static JsonObject ConvertTable(Table table)
    {
        var rows = new JsonArray();
        foreach (var rowObj in table.OfType<TableRow>())
        {
            var cells = new JsonArray();
            foreach (var cell in rowObj.OfType<TableCell>())
            {
                cells.Add(new JsonObject
                {
                    ["type"] = rowObj.IsHeader ? "tableHeader" : "tableCell",
                    ["content"] = NormalizeTableCellContent(ConvertBlocks(cell))
                });
            }

            rows.Add(new JsonObject
            {
                ["type"] = "tableRow",
                ["content"] = cells
            });
        }

        return new JsonObject
        {
            ["type"] = "table",
            ["content"] = rows
        };
    }

    private static JsonArray NormalizeTableCellContent(JsonArray content)
    {
        if (content.Count > 0)
        {
            return content;
        }

        return new JsonArray
        {
            new JsonObject
            {
                ["type"] = "paragraph",
                ["content"] = new JsonArray()
            }
        };
    }

    private static JsonObject CreateCodeBlock(string? info, string text)
    {
        var node = new JsonObject
        {
            ["type"] = "codeBlock",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            }
        };

        var language = NormalizeInfoString(info);
        if (!string.IsNullOrWhiteSpace(language))
        {
            node["attrs"] = new JsonObject { ["language"] = language };
        }

        return node;
    }

    private static string? NormalizeInfoString(string? info)
    {
        if (string.IsNullOrWhiteSpace(info))
        {
            return null;
        }

        var trimmed = info.Trim();
        var firstSpace = trimmed.IndexOf(' ');
        return firstSpace >= 0 ? trimmed[..firstSpace] : trimmed;
    }

    private static JsonObject CreateParagraphFromText(string text)
    {
        return new JsonObject
        {
            ["type"] = "paragraph",
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            }
        };
    }

    private static JsonArray ConvertInlineContainer(ContainerInline? container, IReadOnlyList<JsonObject>? marks = null)
    {
        var nodes = new JsonArray();
        if (container is null)
        {
            return nodes;
        }

        foreach (var inline in container)
        {
            foreach (var node in ConvertInline(inline, marks))
            {
                nodes.Add(node);
            }
        }

        return nodes;
    }

    private static IEnumerable<JsonNode> ConvertInline(Inline inline, IReadOnlyList<JsonObject>? activeMarks)
    {
        switch (inline)
        {
            case LiteralInline literal:
                var text = literal.Content.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    yield return CreateTextNode(text, activeMarks);
                }
                yield break;

            case LineBreakInline breakInline:
                yield return CreateTextNode(breakInline.IsHard ? "\n" : " ", activeMarks);
                yield break;

            case CodeInline code:
                yield return CreateTextNode(code.Content, AddMark(activeMarks, new JsonObject { ["type"] = "code" }));
                yield break;

            case LinkInline link when link.IsImage:
                yield return new JsonObject
                {
                    ["type"] = "image",
                    ["attrs"] = new JsonObject
                    {
                        ["src"] = link.Url ?? string.Empty,
                        ["alt"] = ExtractInlineText(link),
                        ["title"] = string.IsNullOrWhiteSpace(link.Title) ? null : link.Title
                    }
                };
                yield break;

            case LinkInline link:
                var linkMarks = AddMark(activeMarks, new JsonObject
                {
                    ["type"] = "link",
                    ["attrs"] = new JsonObject { ["href"] = link.Url ?? string.Empty }
                });

                if (link.FirstChild is null && !string.IsNullOrWhiteSpace(link.Url))
                {
                    yield return CreateTextNode(link.Url!, linkMarks);
                    yield break;
                }

                foreach (var child in ConvertInlineContainer(link, linkMarks))
                {
                    if (child is not null)
                    {
                        yield return child;
                    }
                }
                yield break;

            case EmphasisInline emphasis:
                var emphasisMarks = AddEmphasisMarks(activeMarks, emphasis);
                foreach (var child in ConvertInlineContainer(emphasis, emphasisMarks))
                {
                    if (child is not null)
                    {
                        yield return child;
                    }
                }
                yield break;

            case HtmlInline htmlInline:
                if (!string.IsNullOrWhiteSpace(htmlInline.Tag))
                {
                    yield return CreateTextNode(htmlInline.Tag, activeMarks);
                }
                yield break;

            default:
                var fallback = inline.ToString();
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    yield return CreateTextNode(fallback, activeMarks);
                }
                yield break;
        }
    }

    private static JsonObject CreateTextNode(string text, IReadOnlyList<JsonObject>? marks)
    {
        var node = new JsonObject
        {
            ["type"] = "text",
            ["text"] = text
        };

        if (marks is { Count: > 0 })
        {
            var marksArray = new JsonArray();
            foreach (var mark in marks)
            {
                marksArray.Add(mark.DeepClone());
            }

            node["marks"] = marksArray;
        }

        return node;
    }

    private static IReadOnlyList<JsonObject> AddEmphasisMarks(IReadOnlyList<JsonObject>? marks, EmphasisInline emphasis)
    {
        var result = marks?.ToList() ?? [];

        if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount >= 2)
        {
            result.Add(new JsonObject { ["type"] = "strike" });
            return result;
        }

        if (emphasis.DelimiterCount >= 2)
        {
            result.Add(new JsonObject { ["type"] = "bold" });
        }
        else
        {
            result.Add(new JsonObject { ["type"] = "italic" });
        }

        return result;
    }

    private static IReadOnlyList<JsonObject> AddMark(IReadOnlyList<JsonObject>? marks, JsonObject mark)
    {
        var result = marks?.ToList() ?? [];
        result.Add(mark);
        return result;
    }

    private static string ExtractInlineText(ContainerInline inline)
    {
        var builder = new StringBuilder();
        foreach (var child in inline)
        {
            switch (child)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case ContainerInline nested:
                    builder.Append(ExtractInlineText(nested));
                    break;
            }
        }

        return builder.ToString();
    }

    private static string ExtractBlockText(Block block)
    {
        return block switch
        {
            LeafBlock leaf when leaf.Lines.Count > 0 => leaf.Lines.ToString().TrimEnd(),
            _ => block.ToString() ?? string.Empty
        };
    }
}
