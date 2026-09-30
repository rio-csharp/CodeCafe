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

namespace CodeCafe.Application.Blocks.InsertBlocks;

public sealed class InsertBlocksCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IUnitOfWork unitOfWork
) : ICommandHandler<InsertBlocksCommand, Result<IReadOnlyList<BlockDto>>>
{
    public async Task<Result<IReadOnlyList<BlockDto>>> Handle(InsertBlocksCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<IReadOnlyList<BlockDto>>(error);
        }

        var (_, page, _, _) = context.Value!;

        // Structural mutation: lock the page row FIRST, then read the chain — computing
        // positions before locking is no lock. The explicit transaction keeps the FOR UPDATE
        // held until commit.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await blocks.LockPageAsync(command.PageId, cancellationToken);
        var pageBlocks = await blocks.ListByPageAsync(command.PageId, cancellationToken);

        var position = BlockPositions.ResolveInsertion(command.AfterBlockId?.ToString(), page, pageBlocks, tempIds: null, moving: null);
        if (!position.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<BlockDto>>(position.Error!);
        }

        var (parent, group, insertIndex) = position.Value!;

        var created = new List<Block>(command.Blocks.Count);
        foreach (var input in command.Blocks)
        {
            var normalized = BlockPayloads.ValidateAndNormalize(input.Type, input.Content);
            if (!normalized.IsSuccess)
            {
                // The transaction is disposed without a commit, rolling the whole batch back.
                return Result.Failure<IReadOnlyList<BlockDto>>(normalized.Error!);
            }

            var sortKey = BlockSiblingSortKeys.KeyForInsert(group, insertIndex);
            var block = Block.Create(
                command.PageId,
                parent?.Id,
                input.Type,
                normalized.Value!.CanonicalJson,
                normalized.Value.PlainText,
                sortKey
            );

            var predecessor = insertIndex > 0 ? group[insertIndex - 1] : null;
            var next = insertIndex < group.Count ? group[insertIndex] : null;
            BlockChain.Insert(block, page, parent, predecessor, next);
            group.Insert(insertIndex, block);
            insertIndex++;

            blocks.Add(block);
            created.Add(block);
        }

        page.Touch();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BlockDto>>(created.Select(BlockMapping.ToDto).ToList());
    }
}
