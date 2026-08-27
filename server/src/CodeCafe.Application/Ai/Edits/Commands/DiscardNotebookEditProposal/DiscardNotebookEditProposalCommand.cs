using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Ai.Edits.Commands.DiscardNotebookEditProposal;

public sealed record DiscardNotebookEditProposalCommand(Guid ProposalId, Guid ActorId)
    : ICommand<AiFlowError?>;
