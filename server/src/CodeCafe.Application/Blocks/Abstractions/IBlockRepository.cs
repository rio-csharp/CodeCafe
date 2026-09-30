using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Abstractions;

public interface IBlockRepository
{
    Task<Block?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    // All blocks of the page; chain surgery happens in memory over the whole set, mirroring
    // GetNotebookTree's whole-tree load.
    Task<IReadOnlyList<Block>> ListByPageAsync(Guid pageId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Block>> ListByPageIdsAsync(IReadOnlyCollection<Guid> pageIds, CancellationToken cancellationToken);

    // SELECT ... FOR UPDATE on the page row. Structural mutations (insert/move/delete/rebalance)
    // call this FIRST, before reading the chain — locking after computing positions is no lock.
    Task LockPageAsync(Guid pageId, CancellationToken cancellationToken);

    void Add(Block block);

    void RemoveRange(IReadOnlyList<Block> blocks);
}
