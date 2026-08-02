using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Notes.Application.Notes;

namespace CodeCafe.Modules.Ai.Edits;

/// <summary>
/// Result of the notebook-edit proposal use cases (create/preview/apply/discard).
/// The endpoint maps <see cref="Proposal"/> to the wire response; <see cref="Applied"/>
/// and <see cref="SavedAtUtc"/> drive the applied/saved response fields.
/// </summary>
public sealed class AiEditProposalFlowResult
{
    public AiNotebookEditProposal? Proposal { get; init; }

    public bool Applied { get; init; }

    public DateTimeOffset? SavedAtUtc { get; init; }

    public AiFlowError? Error { get; init; }

    public bool Succeeded => Error is null;

    public static AiEditProposalFlowResult Success(
        AiNotebookEditProposal proposal,
        bool applied,
        DateTimeOffset? savedAtUtc) =>
        new()
        {
            Proposal = proposal,
            Applied = applied,
            SavedAtUtc = savedAtUtc
        };

    public static AiEditProposalFlowResult Failure(AiFlowError error) =>
        new()
        {
            Error = error
        };

    public static AiEditProposalFlowResult Failure(NotesError error) =>
        Failure(AiFlowError.FromNotesError(error));
}
