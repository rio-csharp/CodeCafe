using System.Text.Json;
using System.Text.RegularExpressions;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Application.Notes;

public static partial class NotebookInput
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

        return TryParseDefinedEnum(value, out visibility);
    }

    // Slugs become URL path segments, so user-supplied ones must match the generator's output shape.
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    public static bool IsValidSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Length is >= NotebookSlug.MinLength and <= NotebookSlug.MaxLength
            && SlugPattern().IsMatch(normalized);
    }

    public static bool TryParseItemType(string value, out NotebookItemType type)
    {
        return TryParseDefinedEnum(value, out type);
    }

    private static bool TryParseDefinedEnum<TEnum>(string value, out TEnum parsedValue)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: true, out parsedValue)
            && Enum.IsDefined(parsedValue);
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
