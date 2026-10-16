using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Domain.Blocks;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class BlockRepository(AppDbContext dbContext) : IBlockRepository
{
    public async Task<Block?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Blocks.FindAsync([id], cancellationToken);

    // Tracked on purpose: handlers restructure the chain in memory and SaveChanges persists the
    // pointer updates, mirroring PageRepository conventions. No ordering here; callers sort.
    public async Task<IReadOnlyList<Block>> ListByPageAsync(Guid pageId, CancellationToken cancellationToken)
        => await dbContext.Blocks
            .Where(block => block.PageId == pageId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Block>> ListByPageIdsAsync(
        IReadOnlyCollection<Guid> pageIds,
        CancellationToken cancellationToken
    )
        => await dbContext.Blocks
            .Where(block => pageIds.Contains(block.PageId))
            .ToListAsync(cancellationToken);

    // Row lock on the page, taken before any structural read. FOR UPDATE only holds for the
    // remainder of an open transaction, so this must run inside one — see AppDbContext notes.
    public async Task LockPageAsync(Guid pageId, CancellationToken cancellationToken)
        => await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM pages WHERE "Id" = {pageId} FOR UPDATE""",
            cancellationToken
        );

    public void Add(Block block) => dbContext.Blocks.Add(block);

    public void RemoveRange(IReadOnlyList<Block> blocks) => dbContext.Blocks.RemoveRange(blocks);
}
