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

namespace CodeCafe.Application.Revisions.ListBlockRevisions;

public sealed class ListBlockRevisionsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IPasswordHasher passwordHasher,
    IBlockRevisionRepository revisions
) : IQueryHandler<ListBlockRevisionsQuery, Result<CursorPage<BlockRevisionDto>>>
{
    internal const int DefaultPageSize = 20;
    internal const int MaxPageSize = 50;

    public async Task<Result<CursorPage<BlockRevisionDto>>> Handle(ListBlockRevisionsQuery query, CancellationToken cancellationToken)
    {
        // History reads need read access; anonymous callers get page_not_found, so a public
        // notebook's edit history never leaks to someone who merely has the link.
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
            return Result.Failure<CursorPage<BlockRevisionDto>>(error);
        }

        DateTimeOffset? cursorCreatedAtUtc = null;
        Guid? cursorId = null;
        if (query.Cursor is not null)
        {
            if (!RevisionCursor.TryDecode(query.Cursor, out var decodedCreatedAtUtc, out var decodedId))
            {
                return Result.Failure<CursorPage<BlockRevisionDto>>(PageErrors.InvalidCursor);
            }

            cursorCreatedAtUtc = decodedCreatedAtUtc;
            cursorId = decodedId;
        }

        var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        // One extra row answers "is there a next page" without a count query.
        var rows = await revisions.ListByBlockAsync(
            query.PageId,
            query.BlockId,
            cursorCreatedAtUtc,
            cursorId,
            pageSize + 1,
            cancellationToken
        );

        var page = rows.Take(pageSize).ToList();
        var nextCursor = rows.Count > pageSize && page.Count > 0
            ? RevisionCursor.Encode(page[^1].CreatedAtUtc, page[^1].Id)
            : null;

        return Result.Success(new CursorPage<BlockRevisionDto>(page.Select(RevisionMapping.ToDto).ToList(), nextCursor));
    }
}
