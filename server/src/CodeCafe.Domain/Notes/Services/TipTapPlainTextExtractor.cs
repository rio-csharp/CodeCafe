using System.Text;
using System.Text.Json;

namespace CodeCafe.Domain.Notes.Services;

internal static class TipTapPlainTextExtractor
{
    public static string? Extract(string? contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(contentJson);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            var builder = new StringBuilder();
            AppendNodeText(document.RootElement, builder);
            var text = builder.ToString().Trim();
            return text.Length == 0 ? null : text;
        }
    }

    private static void AppendNodeText(JsonElement node, StringBuilder builder)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        AppendSelfText(node, builder);

        if (
            node.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.Array
        )
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

    private static void AppendSelfText(JsonElement node, StringBuilder builder)
    {
        if (
            !node.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
        )
        {
            return;
        }

        var type = typeElement.GetString();
        if (
            string.Equals(type, "text", StringComparison.Ordinal)
            && node.TryGetProperty("text", out var textElement)
            && textElement.ValueKind == JsonValueKind.String
        )
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

    private static bool ShouldAppendSeparator(JsonElement node, int index, int totalCount)
    {
        if (index == totalCount - 1)
        {
            return false;
        }

        if (
            !node.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
        )
        {
            return false;
        }

        var type = typeElement.GetString();
        return type is not "text";
    }
}
