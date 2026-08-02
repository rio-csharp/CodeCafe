using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Ai.Edits.Commands.ApplyNotebookEditProposal;

public sealed record ApplyNotebookEditProposalCommand(
    Guid ProposalId,
    Guid ActorId) : ICommand<AiEditProposalFlowResult>;
