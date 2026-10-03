using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Application.Revisions.Shared;

namespace CodeCafe.Application.Blocks.UpdateBlock;

public sealed class UpdateBlockCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IBlockRevisionRepository revisions,
    IChangeSourceAccessor changeSource,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdateBlockCommand, Result<BlockDto>>
{
    public async Task<Result<BlockDto>> Handle(UpdateBlockCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<BlockDto>(error);
        }

        var (_, page, _, _) = context.Value!;

        var block = await blocks.FindByIdAsync(command.BlockId, cancellationToken);
        if (block is null || block.PageId != command.PageId)
        {
            return Result.Failure<BlockDto>(BlockErrors.NotFound);
        }

        // Payload updates ride on optimistic concurrency only — no page lock. The early check
        // answers the common case; the save below catches the race the check cannot see.
        if (block.Version != command.BaseVersion)
        {
            return Result.Failure<BlockDto>(BlockErrors.VersionConflict);
        }

        var normalized = BlockPayloads.ValidateAndNormalize(block.Type, command.Content);
        if (!normalized.IsSuccess)
        {
            return Result.Failure<BlockDto>(normalized.Error!);
        }

        block.UpdateContent(normalized.Value!.CanonicalJson, normalized.Value.PlainText);
        revisions.Add(RevisionRecording.Updated(block, Guid.CreateVersion7(), changeSource.Source));
        page.Touch();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<BlockDto>(BlockErrors.VersionConflict);
        }

        return Result.Success(BlockMapping.ToDto(block));
    }
}
