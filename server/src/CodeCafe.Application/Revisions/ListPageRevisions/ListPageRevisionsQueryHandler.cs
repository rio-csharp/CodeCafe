using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.ListPageRevisions;

// The page history, grouped into batches: one handler transaction = one group. The page-row
// lock taken by every structural write keeps each batch contiguous in the row stream (a payload
// update is a single-row batch), so consecutive rows with the same BatchId are the whole batch.
public sealed class ListPageRevisionsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IPasswordHasher passwordHasher,
    IBlockRevisionRepository revisions
) : IQueryHandler<ListPageRevisionsQuery, Result<CursorPage<PageRevisionGroupDto>>>
{
    internal const int DefaultPageSize = 20;
    internal const int MaxPageSize = 50;

    // A group needs every row of its batch, so the handler scans ROWS until it has seen one
    // batch beyond the page. The cap only exists for pathological batches (a batch whose
    // subtree deletes alone exceed it); hitting it splits that batch across two pages.
    private const int RowScanCap = 1000;
    private const int ChunkSize = 200;

    public async Task<Result<CursorPage<PageRevisionGroupDto>>> Handle(ListPageRevisionsQuery query, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireReadAsync(
            query.PageId,
            accessCode: null,
            currentUserAccessor,
            notebooks,
            pages,
            passwordHasher,
            cancellationToken
        );
        if (context.Error is { } error)
        {
            return Result.Failure<CursorPage<PageRevisionGroupDto>>(error);
        }

        DateTimeOffset? cursorCreatedAtUtc = null;
        Guid? cursorId = null;
        if (query.Cursor is not null)
        {
            if (!RevisionCursor.TryDecode(query.Cursor, out var decodedCreatedAtUtc, out var decodedId))
            {
                return Result.Failure<CursorPage<PageRevisionGroupDto>>(PageErrors.InvalidCursor);
            }

            cursorCreatedAtUtc = decodedCreatedAtUtc;
            cursorId = decodedId;
        }

        var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var rows = new List<BlockRevision>();
        var exhausted = false;
        while (rows.Count < RowScanCap)
        {
            var request = Math.Min(ChunkSize, RowScanCap - rows.Count);
            var chunk = await revisions.ListByPageAsync(
                query.PageId,
                cursorCreatedAtUtc,
                cursorId,
                request,
                cancellationToken
            );
            rows.AddRange(chunk);
            if (chunk.Count < request)
            {
                exhausted = true;
                break;
            }

            cursorCreatedAtUtc = chunk[^1].CreatedAtUtc;
            cursorId = chunk[^1].Id;
            // pageSize + 1 distinct batches: the first pageSize are provably complete.
            if (rows.Select(row => row.BatchId).Distinct().Count() > pageSize)
            {
                break;
            }
        }

        var groups = GroupByBatch(rows);
        // The oldest scanned group is provably complete only when an older batch follows it or
        // the history ran out; otherwise drop it and let the next page refetch it whole. A batch
        // bigger than the scan cap is the one exception: emit it split rather than stall.
        if (!exhausted && groups.Count > 1)
        {
            groups.RemoveAt(groups.Count - 1);
        }

        var page = groups.Take(pageSize).ToList();
        var hasMore = page.Count > 0 && (!exhausted || groups.Count > pageSize);
        var nextCursor = hasMore
            ? RevisionCursor.Encode(page[^1].Rows[^1].CreatedAtUtc, page[^1].Rows[^1].Id)
            : null;

        return Result.Success(new CursorPage<PageRevisionGroupDto>(
            page.Select(group => new PageRevisionGroupDto(
                group.Rows.Max(row => row.CreatedAtUtc),
                group.Rows[0].Source,
                group.Rows.Select(RevisionMapping.ToDto).ToList()
            )).ToList(),
            nextCursor
        ));
    }

    // Rows arrive newest-first with each batch contiguous; groups come out newest-first with
    // their rows flipped back to chronological order.
    private static List<(Guid BatchId, List<BlockRevision> Rows)> GroupByBatch(List<BlockRevision> rows)
    {
        var groups = new List<(Guid, List<BlockRevision>)>();
        foreach (var row in rows)
        {
            if (groups.Count == 0 || groups[^1].Item1 != row.BatchId)
            {
                groups.Add((row.BatchId, []));
            }

            groups[^1].Item2.Add(row);
        }

        foreach (var (_, groupRows) in groups)
        {
            groupRows.Reverse();
        }

        return groups;
    }
}
