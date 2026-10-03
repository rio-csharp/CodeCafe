using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.MoveBlock;

public sealed class MoveBlockCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IBlockRevisionRepository revisions,
    IChangeSourceAccessor changeSource,
    IUnitOfWork unitOfWork
) : ICommandHandler<MoveBlockCommand, Result>
{
    public async Task<Result> Handle(MoveBlockCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var (_, page, _, _) = context.Value!;

        var block = await blocks.FindByIdAsync(command.BlockId, cancellationToken);
        if (block is null || block.PageId != command.PageId)
        {
            return Result.Failure(BlockErrors.NotFound);
        }

        // Structural mutation: lock the page row FIRST, then read the chain — computing
        // positions before locking is no lock. The explicit transaction keeps the FOR UPDATE
        // held until commit.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await blocks.LockPageAsync(command.PageId, cancellationToken);
        var pageBlocks = await blocks.ListByPageAsync(command.PageId, cancellationToken);

        // The moving block is excluded from the target group; its descendants are not — they
        // simply move along with it.
        var position = BlockPositions.ResolveInsertion(command.AfterBlockId?.ToString(), page, pageBlocks, tempIds: null, moving: block);
        if (!position.IsSuccess)
        {
            return Result.Failure(position.Error!);
        }

        var (newParent, group, insertIndex) = position.Value!;
        BlockChain.Move(block, page, newParent, pageBlocks, group, insertIndex);
        revisions.Add(RevisionRecording.Moved(block, Guid.CreateVersion7(), changeSource.Source));

        page.Touch();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
