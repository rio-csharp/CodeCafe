using CodeCafe.Application.Notes;

namespace CodeCafe.Application.Ai;

/// <summary>
/// Failure returned by an AI use-case handler. Genuinely transport-neutral: it carries an
/// <see cref="AiFailureKind"/> rather than an HTTP status code, so a transport decides how to render
/// it. The previous version held an int StatusCode, which put a transport concern in the application
/// layer and is why these handlers could not compile without an AspNetCore reference.
/// </summary>
public sealed record AiFlowError(
    string Code,
    string Message,
    AiFailureKind Kind,
    string? Field = null,
    IReadOnlyDictionary<string, object?>? Details = null)
{
    public static AiFlowError FromNotesError(NotesError error)
        => new(error.Code, error.Message, ToAiFailureKind(error.Kind), error.Field, error.Details);

    /// <summary>
    /// Widens a Notes failure into the AI taxonomy. The AI flows call into Notes use cases, so their
    /// failures have to be expressible here without inventing a status code at the call site.
    /// </summary>
    public static AiFailureKind ToAiFailureKind(NotesFailureKind kind)
        => kind switch
        {
            NotesFailureKind.Validation => AiFailureKind.Validation,
            NotesFailureKind.Forbidden => AiFailureKind.Forbidden,
            NotesFailureKind.NotFound => AiFailureKind.NotFound,
            NotesFailureKind.Conflict => AiFailureKind.Conflict,
            _ => AiFailureKind.Validation
        };
}
