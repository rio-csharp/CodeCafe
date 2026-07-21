using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace CodeCafe.Modules.Notes.Application.Notes;

public static class TipTapDocumentOperations
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static JsonElement AppendBlocks(JsonElement? existingContentJson, JsonElement blocks)
    {
        if (blocks.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Blocks must be a JSON array.", nameof(blocks));
        }

        var (root, content) = GetDocumentContentArray(existingContentJson);
        foreach (var block in blocks.EnumerateArray())
        {
            try
            {
                content.Add(JsonNode.Parse(block.GetRawText()));
            }
            catch (JsonException)
            {
                throw new ArgumentException("Blocks contain an invalid JSON node.", nameof(blocks));
            }
        }

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public static JsonElement ReplaceBlockAtIndex(JsonElement? existingContentJson, int index, JsonElement block)
    {
        var (root, content) = GetDocumentContentArray(existingContentJson);
        if (index < 0 || index >= content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        try
        {
            content[index] = JsonNode.Parse(block.GetRawText());
        }
        catch (JsonException)
        {
            throw new ArgumentException("Block contains invalid JSON.", nameof(block));
        }

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public static JsonElement InsertBlocksAtIndex(JsonElement? existingContentJson, int index, JsonElement blocks)
    {
        if (blocks.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Blocks must be a JSON array.", nameof(blocks));
        }

        var (root, content) = GetDocumentContentArray(existingContentJson);
        if (index < 0 || index > content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        var nodes = new List<JsonNode?>();
        foreach (var block in blocks.EnumerateArray())
        {
            try
            {
                nodes.Add(JsonNode.Parse(block.GetRawText()));
            }
            catch (JsonException)
            {
                throw new ArgumentException("Blocks contain an invalid JSON node.", nameof(blocks));
            }
        }

        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            content.Insert(index, nodes[i]);
        }

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public static JsonElement DeleteBlockAtIndex(JsonElement? existingContentJson, int index)
    {
        var (root, content) = GetDocumentContentArray(existingContentJson);
        if (index < 0 || index >= content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        content.RemoveAt(index);
        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public static JsonElement ReplaceTextInDocument(JsonElement? existingContentJson, string searchText, string replacementText, bool replaceAll)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(searchText));
        }

        replacementText ??= string.Empty;

        var (root, _) = GetDocumentContentArray(existingContentJson);
        var replaced = ReplaceTextInNode(root, searchText, replacementText, replaceAll);
        if (!replaced)
        {
            throw new ArgumentException($"Text '{searchText}' was not found in the document.", nameof(searchText));
        }

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public static JsonElement ApplyOperations(JsonElement? existingContentJson, JsonElement operations)
    {
        if (operations.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Operations must be a JSON array.", nameof(operations));
        }

        var (root, _) = GetDocumentContentArray(existingContentJson);
        foreach (var operation in operations.EnumerateArray())
        {
            ApplyOperationToRoot(root, operation);
        }

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public static int CountTopLevelBlocks(JsonElement? existingContentJson)
    {
        var (_, content) = GetDocumentContentArray(existingContentJson);
        return content.Count;
    }

    public static JsonElement CreateEmptyDocument()
        => JsonSerializer.SerializeToElement(new
        {
            type = "doc",
            content = Array.Empty<object>()
        }, SerializerOptions);

    private static void ApplyOperationToRoot(JsonObject root, JsonElement operation)
    {
        if (operation.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Each operation must be a JSON object.", nameof(operation));
        }

        if (!operation.TryGetProperty("type", out var typeProperty)
            || typeProperty.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("Each operation must include a string type.", nameof(operation));
        }

        var type = typeProperty.GetString()?.Trim().ToLowerInvariant();
        switch (type)
        {
            case "append_blocks":
                AppendBlocksToRoot(root, RequiredProperty(operation, "blocks", JsonValueKind.Array));
                break;
            case "replace_block_at_index":
                ReplaceBlockInRoot(
                    root,
                    RequiredInt(operation, "index"),
                    RequiredProperty(operation, "block", JsonValueKind.Object));
                break;
            case "insert_blocks_at_index":
                InsertBlocksInRoot(
                    root,
                    RequiredInt(operation, "index"),
                    RequiredProperty(operation, "blocks", JsonValueKind.Array));
                break;
            case "delete_block_at_index":
                DeleteBlockInRoot(root, RequiredInt(operation, "index"));
                break;
            case "replace_text":
                ReplaceTextInDocumentInRoot(
                    root,
                    RequiredString(operation, "searchText"),
                    RequiredString(operation, "replacementText"),
                    OptionalBoolean(operation, "replaceAll"));
                break;
            case "replace_text_in_block":
                ReplaceTextInBlockInRoot(
                    root,
                    RequiredInt(operation, "index"),
                    RequiredString(operation, "searchText"),
                    RequiredString(operation, "replacementText"),
                    OptionalBoolean(operation, "replaceAll"));
                break;
            default:
                throw new ArgumentException($"Unsupported operation type '{type}'.", nameof(operation));
        }
    }

    private static JsonArray GetContentArray(JsonObject root)
    {
        if (root["content"] is not JsonArray content)
        {
            throw new ArgumentException("Document 'content' must be a JSON array.");
        }

        return content;
    }

    private static List<JsonNode?> ParseBlocks(JsonElement blocks)
    {
        var nodes = new List<JsonNode?>();
        foreach (var block in blocks.EnumerateArray())
        {
            try
            {
                nodes.Add(JsonNode.Parse(block.GetRawText()));
            }
            catch (JsonException)
            {
                throw new ArgumentException("Blocks contain an invalid JSON node.", nameof(blocks));
            }
        }

        return nodes;
    }

    private static void AppendBlocksToRoot(JsonObject root, JsonElement blocks)
    {
        var content = GetContentArray(root);
        foreach (var node in ParseBlocks(blocks))
        {
            content.Add(node);
        }
    }

    private static void ReplaceBlockInRoot(JsonObject root, int index, JsonElement block)
    {
        var content = GetContentArray(root);
        if (index < 0 || index >= content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        try
        {
            content[index] = JsonNode.Parse(block.GetRawText());
        }
        catch (JsonException)
        {
            throw new ArgumentException("Block contains invalid JSON.", nameof(block));
        }
    }

    private static void InsertBlocksInRoot(JsonObject root, int index, JsonElement blocks)
    {
        var content = GetContentArray(root);
        if (index < 0 || index > content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        var nodes = ParseBlocks(blocks);
        for (var i = nodes.Count - 1; i >= 0; i--)
        {
            content.Insert(index, nodes[i]);
        }
    }

    private static void DeleteBlockInRoot(JsonObject root, int index)
    {
        var content = GetContentArray(root);
        if (index < 0 || index >= content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        content.RemoveAt(index);
    }

    private static void ReplaceTextInDocumentInRoot(JsonObject root, string searchText, string replacementText, bool replaceAll)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(searchText));
        }

        replacementText ??= string.Empty;
        if (!ReplaceTextInNode(root, searchText, replacementText, replaceAll))
        {
            throw new ArgumentException($"Text '{searchText}' was not found in the document.", nameof(searchText));
        }
    }

    private static void ReplaceTextInBlockInRoot(JsonObject root, int index, string searchText, string replacementText, bool replaceAll)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(searchText));
        }

        replacementText ??= string.Empty;
        var content = GetContentArray(root);
        if (index < 0 || index >= content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        if (!ReplaceTextInNode(content[index], searchText, replacementText, replaceAll))
        {
            throw new ArgumentException($"Text '{searchText}' was not found in block {index}.", nameof(searchText));
        }
    }

    public static JsonElement ReplaceTextInBlock(JsonElement? existingContentJson, int index, string searchText, string replacementText, bool replaceAll)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            throw new ArgumentException("Search text cannot be empty.", nameof(searchText));
        }

        replacementText ??= string.Empty;

        var (root, content) = GetDocumentContentArray(existingContentJson);
        if (index < 0 || index >= content.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Block index {index} is out of range. Document has {content.Count} block(s).");
        }

        var block = content[index];
        if (!ReplaceTextInNode(block, searchText, replacementText, replaceAll))
        {
            throw new ArgumentException($"Text '{searchText}' was not found in block {index}.", nameof(searchText));
        }

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    private static (JsonObject Root, JsonArray Content) GetDocumentContentArray(JsonElement? existingContentJson)
    {
        var root = existingContentJson is null || existingContentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? new JsonObject
            {
                ["type"] = "doc",
                ["content"] = new JsonArray()
            }
            : JsonNode.Parse(existingContentJson.Value.GetRawText())?.AsObject()
              ?? new JsonObject
              {
                  ["type"] = "doc",
                  ["content"] = new JsonArray()
              };

        root["type"] ??= "doc";
        if (root["content"] is null)
        {
            root["content"] = new JsonArray();
        }

        if (root["content"] is not JsonArray content)
        {
            throw new ArgumentException("Document 'content' must be a JSON array.", nameof(existingContentJson));
        }

        return (root, content);
    }

    private static JsonElement RequiredProperty(JsonElement operation, string propertyName, JsonValueKind expectedKind)
    {
        if (!operation.TryGetProperty(propertyName, out var property)
            || property.ValueKind != expectedKind)
        {
            throw new ArgumentException($"Operation property '{propertyName}' must be a {expectedKind}.", propertyName);
        }

        return property;
    }

    private static int RequiredInt(JsonElement operation, string propertyName)
    {
        if (!operation.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out var value))
        {
            throw new ArgumentException($"Operation property '{propertyName}' must be an integer.", propertyName);
        }

        return value;
    }

    private static string RequiredString(JsonElement operation, string propertyName)
    {
        if (!operation.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Operation property '{propertyName}' must be a string.", propertyName);
        }

        return property.GetString() ?? string.Empty;
    }

    private static bool OptionalBoolean(JsonElement operation, string propertyName)
    {
        if (!operation.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.True
            || (property.ValueKind == JsonValueKind.False ? false : throw new ArgumentException($"Operation property '{propertyName}' must be a boolean.", propertyName));
    }

    private static bool ReplaceTextInNode(JsonNode? node, string searchText, string replacementText, bool replaceAll)
    {
        var replaced = false;
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("type", out var typeNode)
                && typeNode is JsonValue typeValue
                && typeValue.TryGetValue<string>(out var type)
                && string.Equals(type, "text", StringComparison.Ordinal)
                && obj.TryGetPropertyValue("text", out var textNode)
                && textNode is JsonValue textValue
                && textValue.TryGetValue<string>(out var text))
            {
                var newText = replaceAll
                    ? text.Replace(searchText, replacementText, StringComparison.Ordinal)
                    : ReplaceFirst(text, searchText, replacementText);
                if (!string.Equals(newText, text, StringComparison.Ordinal))
                {
                    obj["text"] = newText;
                    replaced = true;
                    if (!replaceAll)
                    {
                        return true;
                    }
                }
            }

            foreach (var property in obj.ToList())
            {
                if (property.Value is not null && ReplaceTextInNode(property.Value, searchText, replacementText, replaceAll))
                {
                    replaced = true;
                    if (!replaceAll)
                    {
                        return true;
                    }
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (ReplaceTextInNode(item, searchText, replacementText, replaceAll))
                {
                    replaced = true;
                    if (!replaceAll)
                    {
                        return true;
                    }
                }
            }
        }

        return replaced;
    }

    private static string ReplaceFirst(string text, string searchText, string replacementText)
    {
        var index = text.IndexOf(searchText, StringComparison.Ordinal);
        return index < 0
            ? text
            : string.Concat(text.AsSpan(0, index), replacementText, text.AsSpan(index + searchText.Length));
    }
}
