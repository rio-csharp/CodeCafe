using CodeCafe.Application.Notes;
using System.Text;

namespace CodeCafe.Application.Ai;

public static class AiHelpers
{
    public static NotebookContextItemModel? ResolveActivePage(NotebookContextModel notebook, string? activePagePath)
    {
        if (string.IsNullOrWhiteSpace(activePagePath))
        {
            return null;
        }

        var normalizedPath = NotebookInput.NormalizePath(activePagePath);
        return notebook.Items.SingleOrDefault(item =>
            string.Equals(item.Path, normalizedPath, StringComparison.Ordinal)
            && string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeLocale(string? locale)
        => string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim();

    public static string StripCodeFence(string value, string language)
    {
        var trimmed = value.Trim();
        var languageFence = string.Concat("```", language);
        if (trimmed.StartsWith(languageFence, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[languageFence.Length..].TrimStart();
        }
        else if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed["```".Length..].TrimStart();
        }

        if (trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^"```".Length].TrimEnd();
        }

        return trimmed;
    }

    public static string NormalizeAgentName(string agentName)
        => string.IsNullOrWhiteSpace(agentName)
            ? new AiOptions().AgentName
            : agentName.Trim();

    public static string TrimForPrompt(string value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxChars
            ? normalized
            : string.Concat(normalized.AsSpan(0, maxChars), "\n[truncated]");
    }

    public static bool AppendLineWithinBudget(StringBuilder builder, int budget, string value)
    {
        var remaining = budget - builder.Length;
        if (remaining <= 0)
        {
            return false;
        }

        if (value.Length + Environment.NewLine.Length <= remaining)
        {
            builder.AppendLine(value);
            return true;
        }

        if (remaining > "[truncated]".Length + Environment.NewLine.Length)
        {
            builder.Append(value.AsSpan(0, remaining - "[truncated]".Length - Environment.NewLine.Length));
            builder.AppendLine("[truncated]");
        }

        return false;
    }
}
