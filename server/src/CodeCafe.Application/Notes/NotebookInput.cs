using System.Text.Json;
using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Notes;

public static class NotebookInput
{
    public static string? NormalizeSearch(string? search)
    {
        var trimmed = search?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : $"%{trimmed}%";
    }

    public static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static string NormalizePath(string path)
    {
        return path.Trim().Trim('/');
    }

    public static bool TryParseVisibility(string? value, out NotebookVisibility visibility)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            visibility = NotebookVisibility.Private;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out visibility) && Enum.IsDefined(visibility);
    }

    public static bool TryParseItemType(string value, out NotebookItemType type)
    {
        return Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);
    }

    public static bool IsOptionalGuid(JsonElement value)
    {
        return TryParseOptionalGuid(value, out _);
    }

    public static bool TryParseOptionalGuid(JsonElement value, out Guid? guid)
    {
        guid = null;

        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = value.GetString();
        if (!Guid.TryParse(rawValue, out var parsedGuid))
        {
            return false;
        }

        guid = parsedGuid;
        return true;
    }
}
