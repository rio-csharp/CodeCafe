using CodeCafe.Application.Ai;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Ai.Edits.Commands.DiscardNotebookEditProposal;

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
                AiFailureKind.NotFound,
                "proposalId");
        }

        await proposalStore.RemoveAsync(request.ProposalId, cancellationToken);
        return null;
    }
}
