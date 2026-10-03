using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.Abstractions;

public interface IBlockRevisionRepository
{
    void Add(BlockRevision revision);

    void AddRange(IReadOnlyList<BlockRevision> revisions);

    // Every row of one block, any era — restore needs the full per-block history (target row
    // lookup, subtree snapshots), and per-block histories stay short in practice.
    Task<IReadOnlyList<BlockRevision>> ListAllByBlockAsync(Guid pageId, Guid blockId, CancellationToken cancellationToken);

    // Every row of one page — page restore reconstructs the tree from the whole log.
    Task<IReadOnlyList<BlockRevision>> ListAllByPageAsync(Guid pageId, CancellationToken cancellationToken);

    // Newest-first keyset pages for the read endpoints; (before, beforeId) is the exclusive
    // cursor position.
    Task<IReadOnlyList<BlockRevision>> ListByBlockAsync(
        Guid pageId,
        Guid blockId,
        DateTimeOffset? before,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<BlockRevision>> ListByPageAsync(
        Guid pageId,
        DateTimeOffset? before,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken
    );
}
