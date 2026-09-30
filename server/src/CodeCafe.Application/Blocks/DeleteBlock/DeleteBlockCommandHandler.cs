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
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.DeleteBlock;

public sealed class DeleteBlockCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeleteBlockCommand, Result>
{
    public async Task<Result> Handle(DeleteBlockCommand command, CancellationToken cancellationToken)
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

        // Structural mutation: lock the page row before reading the chain, inside one explicit
        // transaction so the FOR UPDATE survives until the delete commits.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await blocks.LockPageAsync(command.PageId, cancellationToken);
        var pageBlocks = await blocks.ListByPageAsync(command.PageId, cancellationToken);

        // DeleteSubtree repairs the external chain and clears every intra-subtree pointer before
        // any row goes away — the chain-pointer FKs are ON DELETE RESTRICT.
        var doomed = BlockChain.DeleteSubtree(block, page, pageBlocks);
        blocks.RemoveRange(doomed);

        page.Touch();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
