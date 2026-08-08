using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Modules.Ai.Edits.Queries.GetNotebookEditProposal;

public sealed record GetNotebookEditProposalQuery(
    Guid ProposalId,
    Guid ActorId) : IQuery<AiEditProposalFlowResult>;
