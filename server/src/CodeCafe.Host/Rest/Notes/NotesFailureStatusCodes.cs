using CodeCafe.Application.Notes;

namespace CodeCafe.Host.Rest.Notes;

/// <summary>
/// Single mapping from <see cref="NotesFailureKind"/> to an HTTP status code for this assembly.
/// </summary>
/// <remarks>
/// The Ai module keeps its own copy in AiHelpers because the mapping cannot live anywhere both can
/// reach without making things worse: Notes.Application is deliberately framework-agnostic (no
/// AspNetCore reference), and putting it in Shared.Presentation would make Shared depend on a business
/// module. NotesFailureStatusCodeParityTests asserts the two stay in agreement.
/// </remarks>
internal static class NotesFailureStatusCodes
{
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
