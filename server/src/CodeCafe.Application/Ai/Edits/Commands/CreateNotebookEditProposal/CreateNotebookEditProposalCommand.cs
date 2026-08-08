using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Ai.Edits.Commands.CreateNotebookEditProposal;

public sealed record CreateNotebookEditProposalCommand(
    Guid ActorId,
    string NotebookSlug,
    string? ActivePagePath,
    string Prompt,
    string? Operation,
    string? Locale,
    bool Apply,
    string? ParentPath,
    DateTimeOffset? ExpectedUpdatedAtUtc) : ICommand<AiEditProposalFlowResult>;
