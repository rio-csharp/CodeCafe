using CodeCafe.Application.Notes;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.Infrastructure.Notes;

public sealed class TipTapContentService(ITipTapPlainTextExtractor plainTextExtractor) : ITipTapContentService
{
    private const int MaxDepth = 64;
    private const int MaxNodeCount = 5000;
    private const int MaxTextLength = 200_000;

    public NotesResult<TipTapContentModel> NormalizePageContent(JsonElement? contentJson)
    {
        if (contentJson is null || contentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return NotesResult<TipTapContentModel>.Success(new TipTapContentModel(null, null));
        }

        var validation = ValidateDocument(contentJson.Value);
        if (!validation.Succeeded)
        {
            return NotesResult<TipTapContentModel>.Failure(validation.Error!.Kind, validation.Error.Code, validation.Error.Message);
        }

        var root = JsonNode.Parse(contentJson.Value.GetRawText())?.AsObject();
        if (root is null)
        {
            return NotesResult<TipTapContentModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_tiptap_document",
                "ContentJson must be a TipTap document object.");
        }

        root["content"] ??= new JsonArray();
        var normalizedJson = root.ToJsonString();

        using var normalizedDocument = JsonDocument.Parse(normalizedJson);
        var plainTextContent = NotesSupport.NormalizeOptionalText(
            plainTextExtractor.Extract(normalizedDocument.RootElement));

        return NotesResult<TipTapContentModel>.Success(new TipTapContentModel(normalizedJson, plainTextContent));
    }

    private static NotesResult ValidateDocument(JsonElement document)
    {
        if (document.ValueKind != JsonValueKind.Object)
        {
            return Invalid("ContentJson must be a TipTap document object.");
        }

        if (!TryGetNodeType(document, out var rootType) || rootType != "doc")
        {
            return Invalid("ContentJson root node must have type 'doc'.");
        }

        if (document.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind != JsonValueKind.Array)
        {
            return Invalid("ContentJson root content must be an array when provided.");
        }

        var state = new ValidationState();
        return ValidateNode(document, depth: 0, state);
    }

    private static NotesResult ValidateNode(JsonElement node, int depth, ValidationState state)
    {
        if (depth > MaxDepth)
        {
            return Invalid("ContentJson is nested too deeply.");
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return Invalid("Every TipTap node must be an object.");
        }

        state.NodeCount++;
        if (state.NodeCount > MaxNodeCount)
        {
            return Invalid("ContentJson contains too many nodes.");
        }

        if (!TryGetNodeType(node, out var type))
        {
            return Invalid("Every TipTap node must have a non-empty string type.");
        }

        if (type == "text")
        {
            if (!node.TryGetProperty("text", out var textElement) || textElement.ValueKind != JsonValueKind.String)
            {
                return Invalid("Text nodes must have string text.");
            }

            state.TextLength += textElement.GetString()?.Length ?? 0;
            if (state.TextLength > MaxTextLength)
            {
                return Invalid("ContentJson text is too large.");
            }
        }

        if (node.TryGetProperty("attrs", out var attrsElement)
            && attrsElement.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null)
        {
            return Invalid("TipTap node attrs must be an object when provided.");
        }

        if (node.TryGetProperty("marks", out var marksElement)
            && marksElement.ValueKind != JsonValueKind.Array)
        {
            return Invalid("TipTap node marks must be an array when provided.");
        }

        if (!node.TryGetProperty("content", out var contentElement))
        {
            return NotesResult.Success();
        }

        if (contentElement.ValueKind != JsonValueKind.Array)
        {
            return Invalid("TipTap node content must be an array when provided.");
        }

        foreach (var child in contentElement.EnumerateArray())
        {
            var validation = ValidateNode(child, depth + 1, state);
            if (!validation.Succeeded)
            {
                return validation;
            }
        }

        return NotesResult.Success();
    }

    private static bool TryGetNodeType(JsonElement node, out string type)
    {
        type = string.Empty;

        if (!node.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        type = typeElement.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(type);
    }

    private static NotesResult Invalid(string message) =>
        NotesResult.Failure(NotesFailureKind.Validation, "invalid_tiptap_document", message);

    private sealed class ValidationState
    {
        public int NodeCount { get; set; }

        public int TextLength { get; set; }
    }
}
