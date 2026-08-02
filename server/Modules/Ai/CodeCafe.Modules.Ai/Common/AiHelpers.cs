using CodeCafe.Modules.Ai.Configuration;
using CodeCafe.Modules.Notes.Application.Notes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CodeCafe.Modules.Ai.Common;

internal static class AiHelpers
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

    public static IResult ToNotesError(NotesError error)
        => ToError(error.Code, error.Message, ToStatusCode(error.Kind), error.Field, error.Details);

    public static IResult ToError(AiFlowError error)
        => ToError(error.Code, error.Message, error.StatusCode, error.Field, error.Details);

    public static IResult ToError(
        string code,
        string message,
        int statusCode,
        string? field = null,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        var problem = new ProblemDetails
        {
            Title = code,
            Detail = message,
            Status = statusCode
        };
        problem.Extensions["code"] = code;
        problem.Extensions["retryable"] = statusCode is StatusCodes.Status429TooManyRequests or StatusCodes.Status502BadGateway;
        if (!string.IsNullOrWhiteSpace(field))
        {
            problem.Extensions["field"] = field;
        }

        if (details is not null)
        {
            problem.Extensions["details"] = details;
        }

        return TypedResults.Problem(problem);
    }

    public static int ToStatusCode(NotesFailureKind kind)
        => kind switch
        {
            NotesFailureKind.Validation => StatusCodes.Status400BadRequest,
            NotesFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            NotesFailureKind.NotFound => StatusCodes.Status404NotFound,
            NotesFailureKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
}
