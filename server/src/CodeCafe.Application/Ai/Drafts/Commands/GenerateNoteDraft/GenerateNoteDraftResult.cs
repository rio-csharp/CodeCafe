using CodeCafe.Application.Ai;

namespace CodeCafe.Application.Ai.Drafts.Commands.GenerateNoteDraft;

public sealed class GenerateNoteDraftResult
{
    public AiNoteDraft? Draft { get; init; }

    public AiFlowError? Error { get; init; }

    public bool Succeeded => Error is null;

    public static GenerateNoteDraftResult Success(AiNoteDraft draft) =>
        new()
        {
            Draft = draft
        };

    public static GenerateNoteDraftResult Failure(AiFlowError error) =>
        new()
        {
            Error = error
        };
}

public sealed record AiNoteDraft(
    string Markdown,
    string Title,
    string Intent,
    string NotebookSlug,
    string? PagePath);
