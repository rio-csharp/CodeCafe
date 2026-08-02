using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Ai.Edits.Queries.GetNotebookEditProposal;

public sealed record GetNotebookEditProposalQuery(
    Guid ProposalId,
    Guid ActorId) : IQuery<AiEditProposalFlowResult>;
