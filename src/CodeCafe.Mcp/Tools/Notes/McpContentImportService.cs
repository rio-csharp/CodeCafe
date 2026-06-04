using CodeCafe.Application.Notes;
using CodeCafe.Mcp.Configuration;
using Markdig;
using Markdig.Extensions.TaskLists;
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
    Task<NotesResult<JsonElement?>> ResolveOptionalPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken);

    Task<NotesResult<JsonElement>> ResolveRequiredPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken);

    Task<NotesResult<JsonElement>> ResolveRequiredBlocksAsync(
        Guid actorId,
        JsonElement? inlineBlocks,
        string? blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken);

    NotesResult EnforcePageContentSize(JsonElement contentJson, string errorCode);

    Task DeleteUploadAsync(Guid actorId, string? uploadId, CancellationToken cancellationToken);
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
    public async Task<NotesResult<JsonElement?>> ResolveOptionalPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken)
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
                "Provide either inline contentJson or contentUploadId, but not both.",
                "contentJson",
                new Dictionary<string, object?>
                {
                    ["conflictingField"] = "contentUploadId"
                });
        }

        var resolved = hasUpload
            ? await ResolveUploadAsPageContentAsync(actorId, contentUploadId!, contentFormat, errorCode, invalidMessage, cancellationToken)
            : ResolveInlineJson(inlineContentJson!.Value, errorCode, invalidMessage, "contentJson");

        if (!resolved.Succeeded)
        {
            return NotesResult<JsonElement?>.Failure(
                resolved.Error!.Kind,
                resolved.Error.Code,
                resolved.Error.Message,
                resolved.Error.Field,
                resolved.Error.Details);
        }

        return NotesResult<JsonElement?>.Success(resolved.Value);
    }

    public async Task<NotesResult<JsonElement>> ResolveRequiredPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        var hasInlineContent = inlineContentJson is not null
            && inlineContentJson.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var hasUpload = !string.IsNullOrWhiteSpace(contentUploadId);

        if (hasInlineContent && hasUpload)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "Provide either inline contentJson or contentUploadId, but not both.",
                "contentJson",
                new Dictionary<string, object?>
                {
                    ["conflictingField"] = "contentUploadId"
                });
        }

        if (hasUpload)
        {
            return await ResolveUploadAsPageContentAsync(actorId, contentUploadId!, contentFormat, errorCode, invalidMessage, cancellationToken);
        }

        if (!hasInlineContent)
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage, "contentJson");
        }

        return ResolveInlineJson(inlineContentJson!.Value, errorCode, invalidMessage, "contentJson");
    }

    public async Task<NotesResult<JsonElement>> ResolveRequiredBlocksAsync(
        Guid actorId,
        JsonElement? inlineBlocks,
        string? blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        var hasInlineBlocks = inlineBlocks is not null
            && inlineBlocks.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
        var hasUpload = !string.IsNullOrWhiteSpace(blocksUploadId);

        if (hasInlineBlocks && hasUpload)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "Provide either inline blocks or blocksUploadId, but not both.",
                "blocks",
                new Dictionary<string, object?>
                {
                    ["conflictingField"] = "blocksUploadId"
                });
        }

        NotesResult<JsonElement> result;
        if (hasUpload)
        {
            result = await ResolveUploadAsBlocksAsync(actorId, blocksUploadId!, blocksFormat, errorCode, invalidMessage, cancellationToken);
        }
        else
        {
            if (!hasInlineBlocks)
            {
                return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage, "blocks");
            }

            result = ResolveInlineJson(inlineBlocks!.Value, errorCode, invalidMessage, "blocks");
        }

        if (!result.Succeeded)
        {
            return result;
        }

        if (result.Value.ValueKind != JsonValueKind.Array)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "Blocks must be a JSON array.",
                "blocks");
        }

        return result;
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
                $"Page content exceeds the limit of {maxBytes} bytes (received {currentBytes} bytes).",
                "contentJson",
                new Dictionary<string, object?>
                {
                    ["maxPageContentBytes"] = maxBytes,
                    ["actualPageContentBytes"] = currentBytes
                });
    }

    public async Task DeleteUploadAsync(Guid actorId, string? uploadId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            return;
        }

        _ = await uploadStore.DeleteAsync(actorId, uploadId, cancellationToken);
    }

    private async Task<NotesResult<JsonElement>> ResolveUploadAsPageContentAsync(
        Guid actorId,
        string contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        var uploadResult = await uploadStore.GetAsync(actorId, contentUploadId, cancellationToken);
        if (!uploadResult.Succeeded)
        {
            return ToNotesResult(uploadResult.Error!, errorCode, "contentUploadId");
        }

        var session = uploadResult.Value!;
        var format = NormalizePageFormat(contentFormat, session.MediaType, session.FileName);
        return format switch
        {
            "tiptap_json" => ParseJsonText(session.ContentText, errorCode, invalidMessage, allowArray: false, field: "contentUploadId"),
            "markdown" => ConvertMarkdownDocument(session.ContentText, errorCode, "contentUploadId"),
            _ => NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "contentFormat must be tiptap_json or markdown.",
                "contentFormat",
                new Dictionary<string, object?>
                {
                    ["supportedFormats"] = new[] { "tiptap_json", "markdown" },
                    ["receivedFormat"] = format
                })
        };
    }

    private async Task<NotesResult<JsonElement>> ResolveUploadAsBlocksAsync(
        Guid actorId,
        string blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken)
    {
        var uploadResult = await uploadStore.GetAsync(actorId, blocksUploadId, cancellationToken);
        if (!uploadResult.Succeeded)
        {
            return ToNotesResult(uploadResult.Error!, errorCode, "blocksUploadId");
        }

        var session = uploadResult.Value!;
        var format = NormalizeBlocksFormat(blocksFormat, session.MediaType, session.FileName);
        return format switch
        {
            "tiptap_blocks_json" => ParseJsonText(session.ContentText, errorCode, invalidMessage, allowArray: true, requireArray: true, field: "blocksUploadId"),
            "markdown" => ConvertMarkdownBlocks(session.ContentText, errorCode, "blocksUploadId"),
            _ => NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                errorCode,
                "blocksFormat must be tiptap_blocks_json or markdown.",
                "blocksFormat",
                new Dictionary<string, object?>
                {
                    ["supportedFormats"] = new[] { "tiptap_blocks_json", "markdown" },
                    ["receivedFormat"] = format
                })
        };
    }

    private NotesResult<JsonElement> ResolveInlineJson(
        JsonElement json,
        string errorCode,
        string invalidMessage,
        string field)
    {
        if (json.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage, field);
        }

        var rawText = json.ValueKind == JsonValueKind.String
            ? json.GetString()
            : json.GetRawText();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage, field);
        }

        var maxBytes = mcpOptionsAccessor.Value.MaxInlineContentBytes;
        var currentBytes = Encoding.UTF8.GetByteCount(rawText);
        if (currentBytes > maxBytes)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                "content_too_large",
                $"Inline content exceeds the limit of {maxBytes} bytes (received {currentBytes} bytes).",
                field,
                new Dictionary<string, object?>
                {
                    ["maxInlineContentBytes"] = maxBytes,
                    ["actualInlineContentBytes"] = currentBytes
                });
        }

        if (json.ValueKind == JsonValueKind.String)
        {
            return ParseJsonText(rawText, errorCode, invalidMessage, allowArray: true, field: field);
        }

        return NotesResult<JsonElement>.Success(json);
    }

    private NotesResult<JsonElement> ConvertMarkdownDocument(string markdown, string errorCode, string field)
    {
        try
        {
            return NotesResult<JsonElement>.Success(markdownImporter.ConvertMarkdownToDocument(markdown));
        }
        catch (Exception ex)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                "markdown_conversion_failed",
                $"Markdown could not be converted: {ex.Message}",
                field,
                new Dictionary<string, object?>
                {
                    ["importFormat"] = "markdown"
                });
        }
    }

    private NotesResult<JsonElement> ConvertMarkdownBlocks(string markdown, string errorCode, string field)
    {
        try
        {
            return NotesResult<JsonElement>.Success(markdownImporter.ConvertMarkdownToBlocks(markdown));
        }
        catch (Exception ex)
        {
            return NotesResult<JsonElement>.Failure(
                NotesFailureKind.Validation,
                "markdown_conversion_failed",
                $"Markdown could not be converted: {ex.Message}",
                field,
                new Dictionary<string, object?>
                {
                    ["importFormat"] = "markdown"
                });
        }
    }

    private static NotesResult<JsonElement> ParseJsonText(
        string rawText,
        string errorCode,
        string invalidMessage,
        bool allowArray,
        bool requireArray = false,
        string? field = null)
    {
        try
        {
            using var document = JsonDocument.Parse(rawText);
            var value = document.RootElement.Clone();
            if (!allowArray && value.ValueKind == JsonValueKind.Array)
            {
                return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage, field);
            }

            if (requireArray && value.ValueKind != JsonValueKind.Array)
            {
                return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, "Blocks must be a JSON array.", field);
            }

            return NotesResult<JsonElement>.Success(value);
        }
        catch (JsonException)
        {
            return NotesResult<JsonElement>.Failure(NotesFailureKind.Validation, errorCode, invalidMessage, field);
        }
    }

    private static NotesResult<JsonElement> ToNotesResult(NotesUploadError error, string fallbackCode, string field)
    {
        return NotesResult<JsonElement>.Failure(
            NotesFailureKind.Validation,
            string.IsNullOrWhiteSpace(error.Code) ? fallbackCode : error.Code,
            error.Message,
            field);
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
        var listItems = list.OfType<ListItemBlock>().ToList();
        if (!list.IsOrdered && listItems.Any(IsTaskListItem))
        {
            return ConvertTaskList(listItems);
        }

        var listType = list.IsOrdered ? "orderedList" : "bulletList";
        var items = new JsonArray();
        foreach (var child in listItems)
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

    private static JsonObject ConvertTaskList(IEnumerable<ListItemBlock> listItems)
    {
        var items = new JsonArray();
        foreach (var child in listItems)
        {
            var isTaskItem = TryGetTaskListItem(child, out var isChecked);
            items.Add(new JsonObject
            {
                ["type"] = "taskItem",
                ["attrs"] = new JsonObject { ["checked"] = isTaskItem && isChecked },
                ["content"] = isTaskItem
                    ? ConvertTaskItemBlocks(child)
                    : ConvertBlocks(child)
            });
        }

        return new JsonObject
        {
            ["type"] = "taskList",
            ["content"] = items
        };
    }

    private static bool IsTaskListItem(ListItemBlock item)
        => TryGetTaskListItem(item, out _);

    private static bool TryGetTaskListItem(ListItemBlock item, out bool isChecked)
    {
        isChecked = false;
        if (item.FirstOrDefault() is not ParagraphBlock paragraph
            || paragraph.Inline?.FirstChild is not TaskList taskList)
        {
            return false;
        }

        isChecked = taskList.Checked;
        return true;
    }

    private static JsonArray ConvertTaskItemBlocks(ListItemBlock item)
    {
        var blocks = new JsonArray();
        var removedTaskMarker = false;
        foreach (var block in item)
        {
            if (!removedTaskMarker && block is ParagraphBlock paragraph && IsTaskParagraph(paragraph))
            {
                blocks.Add(new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = ConvertTaskParagraphInlineContainer(paragraph.Inline)
                });
                removedTaskMarker = true;
                continue;
            }

            foreach (var converted in ConvertBlock(block))
            {
                blocks.Add(converted);
            }
        }

        if (blocks.Count == 0)
        {
            blocks.Add(new JsonObject
            {
                ["type"] = "paragraph",
                ["content"] = new JsonArray()
            });
        }

        return blocks;
    }

    private static bool IsTaskParagraph(ParagraphBlock paragraph)
        => paragraph.Inline?.FirstChild is TaskList;

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
        var content = new JsonArray();
        if (!string.IsNullOrEmpty(text))
        {
            content.Add(new JsonObject
            {
                ["type"] = "text",
                ["text"] = text
            });
        }

        var node = new JsonObject
        {
            ["type"] = "codeBlock",
            ["content"] = content
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
        var content = new JsonArray();
        if (!string.IsNullOrEmpty(text))
        {
            content.Add(new JsonObject
            {
                ["type"] = "text",
                ["text"] = text
            });
        }

        return new JsonObject
        {
            ["type"] = "paragraph",
            ["content"] = content
        };
    }

    private static JsonArray ConvertInlineContainer(ContainerInline? container, IReadOnlyList<JsonObject>? marks = null)
    {
        var nodes = new JsonArray();
        foreach (var node in ConvertInlineChildren(container, marks))
        {
            nodes.Add(node);
        }

        return nodes;
    }

    private static IEnumerable<JsonNode> ConvertInlineChildren(ContainerInline? container, IReadOnlyList<JsonObject>? marks = null)
    {
        if (container is null)
        {
            yield break;
        }

        foreach (var inline in container)
        {
            foreach (var node in ConvertInline(inline, marks))
            {
                yield return node;
            }
        }
    }

    private static JsonArray ConvertTaskParagraphInlineContainer(ContainerInline? container)
    {
        var nodes = new JsonArray();
        if (container is null)
        {
            return nodes;
        }

        var skippedMarker = false;
        var trimmedFirstText = false;
        foreach (var inline in container)
        {
            if (!skippedMarker && inline is TaskList)
            {
                skippedMarker = true;
                continue;
            }

            foreach (var node in ConvertInline(inline, activeMarks: null))
            {
                var convertedNode = trimmedFirstText ? node : TrimLeadingTextNode(node);
                if (convertedNode is null)
                {
                    continue;
                }

                trimmedFirstText = true;
                nodes.Add(convertedNode);
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
                if (breakInline.IsHard)
                {
                    yield return new JsonObject { ["type"] = "hardBreak" };
                    yield break;
                }

                yield return CreateTextNode(" ", activeMarks);
                yield break;

            case CodeInline code:
                if (string.IsNullOrEmpty(code.Content))
                {
                    yield break;
                }

                yield return CreateTextNode(code.Content, AddMark(activeMarks, new JsonObject { ["type"] = "code" }));
                yield break;

            case TaskList taskList:
                yield return CreateTextNode(taskList.Checked ? "[x]" : "[ ]", activeMarks);
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

                foreach (var child in ConvertInlineChildren(link, linkMarks))
                {
                    yield return child;
                }
                yield break;

            case EmphasisInline emphasis:
                var emphasisMarks = AddEmphasisMarks(activeMarks, emphasis);
                foreach (var child in ConvertInlineChildren(emphasis, emphasisMarks))
                {
                    yield return child;
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

    private static JsonNode? TrimLeadingTextNode(JsonNode node)
    {
        if (node is not JsonObject obj
            || !TryGetString(obj, "type", out var type)
            || !string.Equals(type, "text", StringComparison.Ordinal)
            || !TryGetString(obj, "text", out var text))
        {
            return node;
        }

        var trimmed = text.TrimStart();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (string.Equals(trimmed, text, StringComparison.Ordinal))
        {
            return node;
        }

        var clone = obj.DeepClone().AsObject();
        clone["text"] = trimmed;
        return clone;
    }

    private static bool TryGetString(JsonObject obj, string propertyName, out string value)
    {
        value = string.Empty;
        if (obj[propertyName] is not JsonValue jsonValue
            || !jsonValue.TryGetValue<string?>(out var candidate)
            || candidate is null)
        {
            return false;
        }

        value = candidate;
        return true;
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
