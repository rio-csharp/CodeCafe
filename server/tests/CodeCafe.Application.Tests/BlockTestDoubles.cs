using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Tests;

// Blocks are hard-deleted, so unlike StubPageRepository there is no Live filter.
internal sealed class StubBlockRepository : List<Block>, IBlockRepository
{
    public Task<Block?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(this.FirstOrDefault(block => block.Id == id));

    public Task<IReadOnlyList<Block>> ListByPageAsync(Guid pageId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Block>>(this.Where(block => block.PageId == pageId).ToList());

    public Task<IReadOnlyList<Block>> ListByPageIdsAsync(
        IReadOnlyCollection<Guid> pageIds,
        CancellationToken cancellationToken
    )
        => Task.FromResult<IReadOnlyList<Block>>(this.Where(block => pageIds.Contains(block.PageId)).ToList());

    // Row locking is a no-op without a database; tests are single-threaded anyway.
    public Task LockPageAsync(Guid pageId, CancellationToken cancellationToken) => Task.CompletedTask;

    // List<Block>.Add already satisfies IBlockRepository.Add; RemoveRange needs a hide because
    // the base overload takes (index, count).
    public void RemoveRange(IReadOnlyList<Block> blocks)
    {
        foreach (var block in blocks)
        {
            ((List<Block>)this).Remove(block);
        }
    }
}
