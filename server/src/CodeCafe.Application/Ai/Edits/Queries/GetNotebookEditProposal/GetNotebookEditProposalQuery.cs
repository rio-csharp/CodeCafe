using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Ai.Edits.Queries.GetNotebookEditProposal;

public sealed record GetNotebookEditProposalQuery(Guid ProposalId, Guid ActorId)
    : IQuery<AiEditProposalFlowResult>;
