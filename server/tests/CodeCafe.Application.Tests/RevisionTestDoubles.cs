using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests;

// Shared doubles for the revision-aware handler tests.
internal sealed class StubBlockRevisionRepository : List<BlockRevision>, IBlockRevisionRepository
{
    // List<BlockRevision>.Add already satisfies IBlockRevisionRepository.Add.
    public void AddRange(IReadOnlyList<BlockRevision> revisions)
    {
        foreach (var revision in revisions)
        {
            Add(revision);
        }
    }

    public Task<IReadOnlyList<BlockRevision>> ListAllByBlockAsync(Guid pageId, Guid blockId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<BlockRevision>>(Ordered(this.Where(revision => revision.PageId == pageId && revision.BlockId == blockId)));

    public Task<IReadOnlyList<BlockRevision>> ListAllByPageAsync(Guid pageId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<BlockRevision>>(Ordered(this.Where(revision => revision.PageId == pageId)));

    public Task<IReadOnlyList<BlockRevision>> ListByBlockAsync(
        Guid pageId,
        Guid blockId,
        DateTimeOffset? before,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken
    )
        => Task.FromResult<IReadOnlyList<BlockRevision>>(
            ApplyKeyset(this.Where(revision => revision.PageId == pageId && revision.BlockId == blockId), before, beforeId)
                .Take(limit)
                .ToList()
        );

    public Task<IReadOnlyList<BlockRevision>> ListByPageAsync(
        Guid pageId,
        DateTimeOffset? before,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken
    )
        => Task.FromResult<IReadOnlyList<BlockRevision>>(
            ApplyKeyset(this.Where(revision => revision.PageId == pageId), before, beforeId)
                .Take(limit)
                .ToList()
        );

    private static List<BlockRevision> Ordered(IEnumerable<BlockRevision> revisions)
        => revisions
            .OrderByDescending(revision => revision.CreatedAtUtc)
            .ThenByDescending(revision => revision.Id)
            .ToList();

    private static IEnumerable<BlockRevision> ApplyKeyset(IEnumerable<BlockRevision> revisions, DateTimeOffset? before, Guid? beforeId)
    {
        if (before is not null && beforeId is not null)
        {
            // In-memory mirror of the repository's row-value keyset comparison.
            revisions = revisions.Where(revision =>
                ValueTuple.Create(revision.CreatedAtUtc, revision.Id).CompareTo(ValueTuple.Create(before.Value, beforeId.Value)) < 0
            );
        }

        return Ordered(revisions);
    }
}

internal sealed class StubChangeSourceAccessor : IChangeSourceAccessor
{
    public RevisionSource Source { get; set; } = RevisionSource.Human;
}
