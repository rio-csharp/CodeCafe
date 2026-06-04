using CodeCafe.Application.Notes;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.Infrastructure.Notes;

public sealed class TipTapContentService(ITipTapPlainTextExtractor plainTextExtractor) : ITipTapContentService
{
    public const int MaxDepth = 64;
    public const int MaxNodeCount = 5000;
    public const int MaxTextLength = 200_000;

    public NotesResult<TipTapContentModel> NormalizePageContent(JsonElement? contentJson)
    {
        if (contentJson is null || contentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return NotesResult<TipTapContentModel>.Success(new TipTapContentModel(null, null));
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(contentJson.Value.GetRawText()) as JsonObject;
        }
        catch (JsonException)
        {
            root = null;
        }

        if (root is null)
        {
            return NotesResult<TipTapContentModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_tiptap_document",
                "ContentJson must be a TipTap document object.",
                "contentJson");
        }

        root["content"] ??= new JsonArray();
        RemoveEmptyTextNodes(root);
        var normalizedJson = root.ToJsonString();

        using var normalizedDocument = JsonDocument.Parse(normalizedJson);
        var validation = ValidateDocument(normalizedDocument.RootElement);
        if (!validation.Succeeded)
        {
            return NotesResult<TipTapContentModel>.Failure(
                validation.Error!.Kind,
                validation.Error.Code,
                validation.Error.Message,
                validation.Error.Field,
                validation.Error.Details);
        }

        var plainTextContent = NotebookInput.NormalizeOptionalText(
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
            return Invalid(
                "ContentJson is nested too deeply.",
                details: new Dictionary<string, object?>
                {
                    ["maxTipTapDepth"] = MaxDepth,
                    ["actualDepth"] = depth
                });
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return Invalid("Every TipTap node must be an object.");
        }

        state.NodeCount++;
        if (state.NodeCount > MaxNodeCount)
        {
            return Invalid(
                $"ContentJson contains too many nodes. The limit is {MaxNodeCount} nodes; received at least {state.NodeCount}.",
                details: new Dictionary<string, object?>
                {
                    ["maxTipTapNodeCount"] = MaxNodeCount,
                    ["actualTipTapNodeCount"] = state.NodeCount
                });
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

            if (node.TryGetProperty("content", out _))
            {
                return Invalid("Text nodes cannot have child content.");
            }

            state.TextLength += textElement.GetString()?.Length ?? 0;
            if (state.TextLength > MaxTextLength)
            {
                return Invalid(
                    $"ContentJson text is too large. The limit is {MaxTextLength} characters; received at least {state.TextLength}.",
                    details: new Dictionary<string, object?>
                    {
                        ["maxTipTapTextLength"] = MaxTextLength,
                        ["actualTipTapTextLength"] = state.TextLength
                    });
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

        if (node.TryGetProperty("marks", out marksElement))
        {
            foreach (var mark in marksElement.EnumerateArray())
            {
                var validation = ValidateMark(mark);
                if (!validation.Succeeded)
                {
                    return validation;
                }
            }
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

    private static NotesResult ValidateMark(JsonElement mark)
    {
        if (mark.ValueKind != JsonValueKind.Object)
        {
            return Invalid("Every TipTap mark must be an object.");
        }

        if (!TryGetNodeType(mark, out _))
        {
            return Invalid("Every TipTap mark must have a non-empty string type.");
        }

        if (mark.TryGetProperty("attrs", out var attrsElement)
            && attrsElement.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null)
        {
            return Invalid("TipTap mark attrs must be an object when provided.");
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

    private static NotesResult Invalid(
        string message,
        string? field = "contentJson",
        IReadOnlyDictionary<string, object?>? details = null) =>
        NotesResult.Failure(
            NotesFailureKind.Validation,
            "invalid_tiptap_document",
            message,
            field,
            details);

    private static void RemoveEmptyTextNodes(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj["content"] is JsonArray content)
            {
                for (var index = content.Count - 1; index >= 0; index--)
                {
                    var child = content[index];
                    if (child is JsonObject childObject
                        && TryGetString(childObject, "type", out var type)
                        && string.Equals(type, "text", StringComparison.Ordinal)
                        && TryGetString(childObject, "text", out var text)
                        && string.Equals(text, string.Empty, StringComparison.Ordinal))
                    {
                        content.RemoveAt(index);
                        continue;
                    }

                    RemoveEmptyTextNodes(child);
                }
            }

            return;
        }

        if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                RemoveEmptyTextNodes(child);
            }
        }
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

    private sealed class ValidationState
    {
        public int NodeCount { get; set; }

        public int TextLength { get; set; }
    }
}
