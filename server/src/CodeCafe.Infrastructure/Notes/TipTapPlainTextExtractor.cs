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

        AppendSelfText(node, builder);

        if (node.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.Array)
        {
            var childCount = contentElement.GetArrayLength();
            var index = 0;
            foreach (var child in contentElement.EnumerateArray())
            {
                AppendNodeText(child, builder);
                if (ShouldAppendSeparator(child, index, childCount))
                {
                    builder.AppendLine();
                }

                index++;
            }
        }
    }

    internal static void AppendSelfText(JsonElement node, StringBuilder builder)
    {
        if (!node.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String)
        {
            return;
        }

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
        else if (string.Equals(type, "hardBreak", StringComparison.Ordinal))
        {
            builder.AppendLine();
        }
    }

    internal static bool ShouldAppendSeparator(JsonElement node, int index, int totalCount)
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
