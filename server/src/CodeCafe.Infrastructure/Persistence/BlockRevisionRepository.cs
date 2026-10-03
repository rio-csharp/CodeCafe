using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Domain.Revisions;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class BlockRevisionRepository(AppDbContext dbContext) : IBlockRevisionRepository
{
    public void Add(BlockRevision revision) => dbContext.BlockRevisions.Add(revision);

    public void AddRange(IReadOnlyList<BlockRevision> revisions) => dbContext.BlockRevisions.AddRange(revisions);

    public async Task<IReadOnlyList<BlockRevision>> ListAllByBlockAsync(Guid pageId, Guid blockId, CancellationToken cancellationToken)
        => await dbContext.BlockRevisions
            .Where(revision => revision.PageId == pageId && revision.BlockId == blockId)
            .OrderByDescending(revision => revision.CreatedAtUtc)
            .ThenByDescending(revision => revision.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BlockRevision>> ListAllByPageAsync(Guid pageId, CancellationToken cancellationToken)
        => await dbContext.BlockRevisions
            .Where(revision => revision.PageId == pageId)
            .OrderByDescending(revision => revision.CreatedAtUtc)
            .ThenByDescending(revision => revision.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BlockRevision>> ListByBlockAsync(
        Guid pageId,
        Guid blockId,
        DateTimeOffset? before,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken
    )
        => await ApplyKeyset(
                dbContext.BlockRevisions.Where(revision => revision.PageId == pageId && revision.BlockId == blockId),
                before,
                beforeId
            )
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BlockRevision>> ListByPageAsync(
        Guid pageId,
        DateTimeOffset? before,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken
    )
        => await ApplyKeyset(dbContext.BlockRevisions.Where(revision => revision.PageId == pageId), before, beforeId)
            .Take(limit)
            .ToListAsync(cancellationToken);

    // Strictly older than the cursor position, as one row-value comparison — the Npgsql
    // translation for keyset pagination ((created_at_utc, id) < (@before, @beforeId)), matching
    // the descending ORDER BY on the same pair.
    private static IQueryable<BlockRevision> ApplyKeyset(IQueryable<BlockRevision> query, DateTimeOffset? before, Guid? beforeId)
    {
        if (before is not null && beforeId is not null)
        {
            query = query.Where(revision =>
                EF.Functions.LessThan(
                    ValueTuple.Create(revision.CreatedAtUtc, revision.Id),
                    ValueTuple.Create(before.Value, beforeId.Value)
                )
            );
        }

        return query
            .OrderByDescending(revision => revision.CreatedAtUtc)
            .ThenByDescending(revision => revision.Id);
    }
}
