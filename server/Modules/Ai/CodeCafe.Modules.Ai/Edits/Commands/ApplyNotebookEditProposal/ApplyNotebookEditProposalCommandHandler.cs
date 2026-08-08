using CodeCafe.Modules.Ai.Common;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;
using CodeCafe.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Application.Common.Messaging;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.Modules.Ai.Edits.Commands.ApplyNotebookEditProposal;

public sealed class ApplyNotebookEditProposalCommandHandler(
    INotebookReadService notebookReadService,
    ISender sender,
    IAiNotebookEditProposalStore proposalStore)
    : ICommandHandler<ApplyNotebookEditProposalCommand, AiEditProposalFlowResult>
{
    public async Task<AiEditProposalFlowResult> Handle(
        ApplyNotebookEditProposalCommand request,
        CancellationToken cancellationToken)
    {
        // Consuming is the atomic claim on the proposal: a racing apply observes the row
        // already deleted and gets the same 404 as a missing proposal.
        var proposal = await proposalStore.TryConsumeAsync(request.ProposalId, request.ActorId, cancellationToken);
        if (proposal is null)
        {
            return AiEditProposalFlowResult.Failure(new AiFlowError(
                "ai_edit_proposal_not_found",
                "The notebook edit proposal was not found or has expired.",
                StatusCodes.Status404NotFound,
                "proposalId"));
        }

        var result = await ApplyProposalAsync(proposal, request.ActorId, cancellationToken);
        if (!result.Succeeded)
        {
            // The proposal was consumed but could not be applied; restore it so the user can retry.
            // Only typed error results reach this path: a thrown exception means a bug and
            // deliberately skips the restore.
            try
            {
                await proposalStore.SaveAsync(proposal, cancellationToken);
            }
            catch
            {
                // Best effort only: when the restore fails the proposal is lost, same as a crash.
            }
        }

        return result;
    }

    private async Task<AiEditProposalFlowResult> ApplyProposalAsync(
        AiNotebookEditProposal proposal,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var notebookResult = await notebookReadService.GetNotebookContextAsync(
            proposal.NotebookSlug,
            actorId,
            cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return AiEditProposalFlowResult.Failure(notebookResult.Error!);
        }

        var notebook = notebookResult.Value!;
        DateTimeOffset? savedAtUtc;
        string? pagePath = proposal.PagePath;
        Guid? pageId = proposal.PageId;
        string? parentPath = proposal.ParentPath;

        if (proposal.EffectiveOperation == "create_page")
        {
            var parent = NotebookContextTree.ResolveParentForCreate(notebook, proposal.ParentPath);
            if (!parent.Succeeded)
            {
                return AiEditProposalFlowResult.Failure(parent.Error!);
            }

            var createResult = await sender.Send(
                new CreateNotebookItemCommand(
                    notebook.Id,
                    actorId,
                    parent.Value?.Id,
                    "page",
                    proposal.Title,
                    NotebookContextTree.ResolveCreateSortOrder(notebook, parent.Value?.Id),
                    proposal.AfterContentJson),
                cancellationToken);
            if (!createResult.Succeeded)
            {
                return AiEditProposalFlowResult.Failure(createResult.Error!);
            }

            var createdPage = createResult.Value!;
            pageId = createdPage.Id;
            pagePath = createdPage.Path;
            parentPath = NotebookContextTree.ResolveParentPathFromItem(notebook, createdPage.ParentId);
            savedAtUtc = createdPage.UpdatedAtUtc ?? createdPage.CreatedAtUtc;
        }
        else if (proposal.EffectiveOperation == "delete_page")
        {
            if (proposal.PageId is null)
            {
                return ActivePageRequired();
            }

            var archiveResult = await sender.Send(
                new ArchiveNotebookItemCommand(
                    notebook.Id,
                    proposal.PageId.Value,
                    actorId),
                cancellationToken);
            if (!archiveResult.Succeeded)
            {
                return AiEditProposalFlowResult.Failure(archiveResult.Error!);
            }

            var archivedPage = archiveResult.Value!;
            pageId = archivedPage.Id;
            pagePath = archivedPage.Path;
            savedAtUtc = archivedPage.UpdatedAtUtc ?? archivedPage.CreatedAtUtc;
        }
        else
        {
            if (proposal.PageId is null)
            {
                return ActivePageRequired();
            }

            var updateResult = await sender.Send(
                new UpdateNotebookItemCommand(
                    notebook.Id,
                    proposal.PageId.Value,
                    actorId,
                    proposal.Title,
                    default,
                    null,
                    proposal.AfterContentJson,
                    proposal.SourcePageUpdatedAtUtc),
                cancellationToken);
            if (!updateResult.Succeeded)
            {
                return AiEditProposalFlowResult.Failure(updateResult.Error!);
            }

            var updatedPage = updateResult.Value!;
            pageId = updatedPage.Id;
            pagePath = updatedPage.Path;
            savedAtUtc = updatedPage.UpdatedAtUtc ?? updatedPage.CreatedAtUtc;
        }

        var appliedProposal = proposal with
        {
            PageId = pageId,
            PagePath = pagePath,
            ParentPath = parentPath
        };

        return AiEditProposalFlowResult.Success(appliedProposal, applied: true, savedAtUtc);
    }

    private static AiEditProposalFlowResult ActivePageRequired()
        => AiEditProposalFlowResult.Failure(new AiFlowError(
            "active_page_required",
            "An active page is required for this AI edit operation.",
            StatusCodes.Status400BadRequest,
            "activePagePath"));
}
