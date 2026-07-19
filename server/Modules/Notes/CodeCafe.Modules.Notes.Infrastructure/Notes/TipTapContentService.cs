using CodeCafe.Modules.Notes.Application.Notes;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using static CodeCafe.Modules.Notes.Application.Notes.ITipTapContentService;

namespace CodeCafe.Modules.Notes.Infrastructure.Notes;

public sealed class TipTapContentService : ITipTapContentService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public NotesResult<TipTapContentModel> NormalizePageContent(JsonElement? contentJson)
    {
        if (contentJson is null || contentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return NotesResult<TipTapContentModel>.Success(new TipTapContentModel(null, null));
        }

        var rawJson = contentJson.Value.GetRawText();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawJson);
        }
        catch (JsonException)
        {
            return InvalidDocument();
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return InvalidDocument();
            }

            // Single recursive walk: validates the document, detects whether a rewrite is
            // required (empty text nodes, missing root content, unsafe URLs), and
            // accumulates the plain text of the normalized document.
            var state = new ValidationState();
            var validation = ProcessDocument(document.RootElement, state);
            if (!validation.Succeeded)
            {
                return NotesResult<TipTapContentModel>.Failure(
                    validation.Error!.Kind,
                    validation.Error.Code,
                    validation.Error.Message,
                    validation.Error.Field,
                    validation.Error.Details);
            }

            var normalizedJson = state.RequiresRewrite
                ? RewriteDocument(rawJson)
                : JsonSerializer.Serialize(document.RootElement, SerializerOptions);
            var plainTextContent = NotebookInput.NormalizeOptionalText(state.PlainText.ToString());

            return NotesResult<TipTapContentModel>.Success(new TipTapContentModel(normalizedJson, plainTextContent));
        }
    }

    private static NotesResult<TipTapContentModel> InvalidDocument() =>
        NotesResult<TipTapContentModel>.Failure(
            NotesFailureKind.Validation,
            "invalid_tiptap_document",
            "ContentJson must be a TipTap document object.",
            "contentJson");

    private static NotesResult ProcessDocument(JsonElement document, ValidationState state)
    {
        if (document.ValueKind != JsonValueKind.Object)
        {
            return Invalid("ContentJson must be a TipTap document object.");
        }

        if (!TryGetNodeType(document, out var rootType) || rootType != "doc")
        {
            return Invalid("ContentJson root node must have type 'doc'.");
        }

        if (document.TryGetProperty("content", out var contentElement))
        {
            if (contentElement.ValueKind == JsonValueKind.Null)
            {
                // A null root content is normalized to an empty array.
                state.RequiresRewrite = true;
            }
            else if (contentElement.ValueKind != JsonValueKind.Array)
            {
                return Invalid("ContentJson root content must be an array when provided.");
            }
        }
        else
        {
            // A missing root content is normalized to an empty array.
            state.RequiresRewrite = true;
        }

        return ProcessNode(document, depth: 0, state, isRootNode: true);
    }

    private static NotesResult ProcessNode(JsonElement node, int depth, ValidationState state, bool isRootNode = false)
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

        if (type is "image" or "youtube"
            && attrsElement.ValueKind == JsonValueKind.Object
            && attrsElement.TryGetProperty("src", out var srcElement)
            && srcElement.ValueKind == JsonValueKind.String
            && !ContentUrlPolicy.IsAllowedResourceUrl(srcElement.GetString()))
        {
            // The rewrite removes unsafe src attributes from image and embed nodes.
            state.RequiresRewrite = true;
        }

        if (node.TryGetProperty("marks", out var marksElement))
        {
            if (marksElement.ValueKind != JsonValueKind.Array)
            {
                return Invalid("TipTap node marks must be an array when provided.");
            }

            foreach (var mark in marksElement.EnumerateArray())
            {
                var validation = ProcessMark(mark, state);
                if (!validation.Succeeded)
                {
                    return validation;
                }
            }
        }

        TipTapPlainTextExtractor.AppendSelfText(node, state.PlainText);

        if (!node.TryGetProperty("content", out var contentElement))
        {
            return NotesResult.Success();
        }

        if (contentElement.ValueKind == JsonValueKind.Null)
        {
            // Only the root node accepts null content; it is normalized to an empty array.
            return isRootNode
                ? NotesResult.Success()
                : Invalid("TipTap node content must be an array when provided.");
        }

        if (contentElement.ValueKind != JsonValueKind.Array)
        {
            return Invalid("TipTap node content must be an array when provided.");
        }

        var keptChildCount = CountKeptChildren(contentElement, state);
        var keptIndex = 0;
        foreach (var child in contentElement.EnumerateArray())
        {
            if (IsEmptyTextNode(child))
            {
                continue;
            }

            var validation = ProcessNode(child, depth + 1, state);
            if (!validation.Succeeded)
            {
                return validation;
            }

            keptIndex++;
            if (TipTapPlainTextExtractor.ShouldAppendSeparator(child, keptIndex - 1, keptChildCount))
            {
                state.PlainText.AppendLine();
            }
        }

        return NotesResult.Success();
    }

    private static NotesResult ProcessMark(JsonElement mark, ValidationState state)
    {
        if (mark.ValueKind != JsonValueKind.Object)
        {
            return Invalid("Every TipTap mark must be an object.");
        }

        if (!TryGetNodeType(mark, out var markType))
        {
            return Invalid("Every TipTap mark must have a non-empty string type.");
        }

        if (!mark.TryGetProperty("attrs", out var attrsElement))
        {
            return NotesResult.Success();
        }

        if (attrsElement.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null)
        {
            return Invalid("TipTap mark attrs must be an object when provided.");
        }

        if (markType == "link"
            && attrsElement.ValueKind == JsonValueKind.Object
            && attrsElement.TryGetProperty("href", out var hrefElement)
            && hrefElement.ValueKind == JsonValueKind.String
            && !ContentUrlPolicy.IsAllowedLinkUrl(hrefElement.GetString()))
        {
            // The rewrite removes link marks with unsafe hrefs but keeps their text.
            state.RequiresRewrite = true;
        }

        return NotesResult.Success();
    }

    private static int CountKeptChildren(JsonElement contentElement, ValidationState state)
    {
        var count = 0;
        foreach (var child in contentElement.EnumerateArray())
        {
            if (IsEmptyTextNode(child))
            {
                // Empty text nodes are removed by the rewrite.
                state.RequiresRewrite = true;
            }
            else
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsEmptyTextNode(JsonElement node)
    {
        return node.ValueKind == JsonValueKind.Object
            && node.TryGetProperty("type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String
            && string.Equals(typeElement.GetString(), "text", StringComparison.Ordinal)
            && node.TryGetProperty("text", out var textElement)
            && textElement.ValueKind == JsonValueKind.String
            && string.Equals(textElement.GetString(), string.Empty, StringComparison.Ordinal);
    }

    private static string RewriteDocument(string rawJson)
    {
        // The document already passed validation, so this parse always succeeds and the
        // root is always an object.
        var root = JsonNode.Parse(rawJson)!.AsObject();
        root["content"] ??= new JsonArray();
        RemoveEmptyTextNodes(root);
        RemoveUnsafeUrls(root);
        return root.ToJsonString(SerializerOptions);
    }

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

    private static void RemoveUnsafeUrls(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj["marks"] is JsonArray marks)
            {
                for (var index = marks.Count - 1; index >= 0; index--)
                {
                    if (marks[index] is JsonObject mark
                        && TryGetString(mark, "type", out var markType)
                        && string.Equals(markType, "link", StringComparison.Ordinal)
                        && mark["attrs"] is JsonObject linkAttrs
                        && TryGetString(linkAttrs, "href", out var href)
                        && !ContentUrlPolicy.IsAllowedLinkUrl(href))
                    {
                        marks.RemoveAt(index);
                    }
                }
            }

            if (TryGetString(obj, "type", out var nodeType)
                && (string.Equals(nodeType, "image", StringComparison.Ordinal)
                    || string.Equals(nodeType, "youtube", StringComparison.Ordinal))
                && obj["attrs"] is JsonObject attrs
                && TryGetString(attrs, "src", out var src)
                && !ContentUrlPolicy.IsAllowedResourceUrl(src))
            {
                attrs.Remove("src");
            }

            if (obj["content"] is JsonArray content)
            {
                foreach (var child in content)
                {
                    RemoveUnsafeUrls(child);
                }
            }

            return;
        }

        if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                RemoveUnsafeUrls(child);
            }
        }
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

        public bool RequiresRewrite { get; set; }

        public StringBuilder PlainText { get; } = new();
    }
}
