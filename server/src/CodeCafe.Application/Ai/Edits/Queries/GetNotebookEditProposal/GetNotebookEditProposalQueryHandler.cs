using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notes;

namespace CodeCafe.Application.Ai.Edits.Queries.GetNotebookEditProposal;

public sealed class GetNotebookEditProposalQueryHandler(
    IAiNotebookEditProposalStore proposalStore,
    INotebookReadService notebookReadService
) : IQueryHandler<GetNotebookEditProposalQuery, AiEditProposalFlowResult>
{
    public async Task<AiEditProposalFlowResult> Handle(
        GetNotebookEditProposalQuery request,
        CancellationToken cancellationToken
    )
    {
        var proposal = await proposalStore.TryGetAsync(
            request.ProposalId,
            request.ActorId,
            cancellationToken
        );
        if (proposal is null)
        {
            return AiEditProposalFlowResult.Failure(
                new AiFlowError(
                    "ai_edit_proposal_not_found",
                    "The notebook edit proposal was not found or has expired.",
                    AiFailureKind.NotFound,
                    "proposalId"
                )
            );
        }

        var notebookResult = await notebookReadService.GetNotebookContextAsync(
            proposal.NotebookSlug,
            request.ActorId,
            cancellationToken
        );
        if (!notebookResult.Succeeded)
        {
            return AiEditProposalFlowResult.Failure(notebookResult.Error!);
        }

        if (
            proposal.PagePath is not null
            && proposal.EffectiveOperation != "create_page"
            && AiHelpers.ResolveActivePage(notebookResult.Value!, proposal.PagePath) is null
        )
        {
            return AiEditProposalFlowResult.Failure(
                new AiFlowError(
                    "notebook_item_not_found",
                    "Notebook item was not found.",
                    AiFailureKind.NotFound,
                    "pagePath"
                )
            );
        }

        return AiEditProposalFlowResult.Success(proposal, applied: false, savedAtUtc: null);
    }
}
