using System.Text.Json;

using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.GetPageAtRevision;

// Reconstructs the page's block state at a past instant — the same "latest row per block at
// or before that instant" computation the restore command applies, but read-only, so the
// reader can preview (and diff) a version before restoring it.
public sealed class GetPageAtRevisionQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IPasswordHasher passwordHasher,
    IBlockRevisionRepository revisions
) : IQueryHandler<GetPageAtRevisionQuery, Result<PageRevisionSnapshotDto>>
{
    public async Task<Result<PageRevisionSnapshotDto>> Handle(GetPageAtRevisionQuery query, CancellationToken cancellationToken)
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
            return Result.Failure<PageRevisionSnapshotDto>(error);
        }

        var history = await revisions.ListAllByPageAsync(query.PageId, cancellationToken);

        var blocks = history
            .Where(row => row.CreatedAtUtc <= query.AtUtc)
            .GroupBy(row => row.BlockId)
            .Select(group => group.OrderByDescending(row => row.CreatedAtUtc).ThenByDescending(row => row.Id).First())
            .Where(row => row.ChangeKind != BlockChangeKind.Deleted)
            .Select(row => new PageRevisionBlockDto(
                row.BlockId,
                row.ParentBlockId,
                row.Type,
                JsonSerializer.Deserialize<JsonElement>(row.ContentJson),
                row.SortKey,
                row.BlockVersion,
                row.CreatedAtUtc
            ))
            .OrderBy(block => block.SortKey, StringComparer.Ordinal)
            .ToList();

        return Result.Success(new PageRevisionSnapshotDto(query.AtUtc, blocks));
    }
}
