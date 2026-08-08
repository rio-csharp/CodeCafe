using CodeCafe.Application.Notes;

namespace CodeCafe.Modules.Ai.Common;

/// <summary>
/// Transport-neutral error for AI use-case handlers. Endpoints map it to a
/// problem-details response via <see cref="AiHelpers.ToError(AiFlowError)"/> so
/// response shapes stay identical to the pre-MediatR endpoints.
/// </summary>
public sealed record AiFlowError(
    string Code,
    string Message,
    int StatusCode,
    string? Field = null,
    IReadOnlyDictionary<string, object?>? Details = null)
{
    public static AiFlowError FromNotesError(NotesError error)
        => new(error.Code, error.Message, AiHelpers.ToStatusCode(error.Kind), error.Field, error.Details);
}
