using CodeCafe.Application.Notes;
using System.Text;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Notes;

public sealed class TipTapPlainTextExtractor : ITipTapPlainTextExtractor
{
    public string? Extract(JsonElement? contentJson)
    {
        if (contentJson is null || contentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var builder = new StringBuilder();
        AppendNodeText(contentJson.Value, builder);

        var text = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void AppendNodeText(JsonElement node, StringBuilder builder)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (node.TryGetProperty("type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String)
        {
            var type = typeElement.GetString();
            if (string.Equals(type, "text", StringComparison.Ordinal)
                && node.TryGetProperty("text", out var textElement)
                && textElement.ValueKind == JsonValueKind.String)
            {
                builder.Append(textElement.GetString());
            }
            else if (string.Equals(type, "image", StringComparison.Ordinal))
            {
                builder.Append("[Image]");
            }
            else if (string.Equals(type, "youtube", StringComparison.Ordinal))
            {
                builder.Append("[Video]");
            }
        }

        if (node.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.Array)
        {
            var childNodes = contentElement.EnumerateArray().ToArray();
            for (var index = 0; index < childNodes.Length; index++)
            {
                AppendNodeText(childNodes[index], builder);
                if (ShouldAppendSeparator(childNodes[index], index, childNodes.Length))
                {
                    builder.AppendLine();
                }
            }
        }
    }

    private static bool ShouldAppendSeparator(JsonElement node, int index, int totalCount)
    {
        if (index == totalCount - 1)
        {
            return false;
        }

        if (!node.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var type = typeElement.GetString();
        return type is not "text";
    }
}
