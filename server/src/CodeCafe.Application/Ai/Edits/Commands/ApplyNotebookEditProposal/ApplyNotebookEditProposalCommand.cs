using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Ai.Edits.Commands.ApplyNotebookEditProposal;

public sealed record ApplyNotebookEditProposalCommand(
    Guid ProposalId,
    Guid ActorId) : ICommand<AiEditProposalFlowResult>;
