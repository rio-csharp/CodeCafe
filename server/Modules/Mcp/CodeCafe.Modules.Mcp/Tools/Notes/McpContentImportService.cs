using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Shared.Application.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

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

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase);
    }
}
