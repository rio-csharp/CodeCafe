using CodeCafe.Modules.Ai.Common;
using CodeCafe.Application.Common.Messaging;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.Modules.Ai.Edits.Commands.DiscardNotebookEditProposal;

public sealed class DiscardNotebookEditProposalCommandHandler(
    IAiNotebookEditProposalStore proposalStore)
    : ICommandHandler<DiscardNotebookEditProposalCommand, AiFlowError?>
{
    public async Task<AiFlowError?> Handle(
        DiscardNotebookEditProposalCommand request,
        CancellationToken cancellationToken)
    {
        var proposal = await proposalStore.TryGetAsync(request.ProposalId, request.ActorId, cancellationToken);
        if (proposal is null)
        {
            return new AiFlowError(
                "ai_edit_proposal_not_found",
                "The notebook edit proposal was not found or has expired.",
                StatusCodes.Status404NotFound,
                "proposalId");
        }

        await proposalStore.RemoveAsync(request.ProposalId, cancellationToken);
        return null;
    }
}
