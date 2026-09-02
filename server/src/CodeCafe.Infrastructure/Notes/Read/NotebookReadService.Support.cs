using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.ValueObjects;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Read;

public sealed partial class NotebookReadService
{
    private async Task<Notebook?> GetPublicNotebookEntityBySlugAsync(
        string slug,
        CancellationToken cancellationToken
    )
    {
        if (!NotesSupport.TryCreateSlug(slug, out var slugValue))
        {
            return null;
        }

        return await dbContext
            .Notebooks.AsNoTracking()
            .SingleOrDefaultAsync(
                notebook =>
                    notebook.Slug == slugValue
                    && notebook.Visibility == NotebookVisibility.Public,
                cancellationToken
            );
    }

    private async Task<NotesResult<NotebookAccessRow>> GetPublicReadableNotebookAccessAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        string? accessCode = null
    )
    {
        if (!NotesSupport.TryCreateSlug(slug, out var slugValue))
        {
            return NotesResult<NotebookAccessRow>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."
            );
        }

        var notebook = await BuildAccessRowQuery(
                dbContext.Notebooks.AsNoTracking()
                    .Where(existingNotebook =>
                        existingNotebook.Slug == slugValue
                        && existingNotebook.Visibility == NotebookVisibility.Public
                    ),
                currentUserId
            )
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId, accessCode);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetReadableNotebookAccessAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        string? accessCode = null
    )
    {
        var notebook = await BuildAccessRowQuery(
                dbContext.Notebooks.AsNoTracking()
                    .Where(existingNotebook => existingNotebook.Id == notebookId),
                currentUserId
            )
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId, accessCode);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetReadableNotebookAccessAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        string? accessCode = null
    )
    {
        if (!NotesSupport.TryCreateSlug(slug, out var slugValue))
        {
            return NotesResult<NotebookAccessRow>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."
            );
        }

        var notebook = await BuildAccessRowQuery(
                dbContext.Notebooks.AsNoTracking()
                    .Where(existingNotebook => existingNotebook.Slug == slugValue),
                currentUserId
            )
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId, accessCode);
    }

    private IQueryable<NotebookAccessRow> BuildAccessRowQuery(
        IQueryable<Notebook> query,
        Guid currentUserId
    )
    {
        return query.Select(notebook => new NotebookAccessRow(
            notebook.Id,
            notebook.OwnerId,
            notebook.Visibility,
            notebook.AccessCodeHash,
            currentUserId != Guid.Empty
                && dbContext.NotebookShares.Any(share =>
                    share.NotebookId == notebook.Id && share.UserId == currentUserId
                )
        ));
    }

    private NotesResult<NotebookAccessRow> ToReadableNotebookAccessResult(
        NotebookAccessRow? notebook,
        Guid currentUserId,
        string? accessCode = null
    )
    {
        if (notebook is null)
        {
            return NotesResult<NotebookAccessRow>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."
            );
        }

        var accessCodeVerified =
            notebook.AccessCodeHash is not null
            && accessCode is not null
            && accessCodeHasher.Verify(notebook.AccessCodeHash, accessCode);

        var context = new NotebookAccessContext(
            notebook.OwnerId,
            notebook.Visibility,
            notebook.AccessCodeHash is not null,
            notebook.IsSharedWithUser,
            accessCodeVerified
        );

        if (NotebookAccessPolicy.CanReadNotebook(context, currentUserId))
        {
            return NotesResult<NotebookAccessRow>.Success(notebook);
        }

        // A coded unlisted notebook tells the caller a code exists — its existence is
        // public knowledge already (the link circulated), so no enumeration leak here.
        return NotebookAccessPolicy.RequiresAccessCode(context, currentUserId)
            ? NotesResult<NotebookAccessRow>.Failure(
                NotesFailureKind.Forbidden,
                "access_code_required",
                "An access code is required to view this notebook."
            )
            : NotesResult<NotebookAccessRow>.Failure(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "You do not have access to this notebook."
            );
    }

    private static NotesError? GetArchivedReadError(
        NotebookAccessRow notebook,
        Guid currentUserId,
        bool includeArchived
    )
    {
        return includeArchived && notebook.OwnerId != currentUserId
            ? new NotesError(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "Only the notebook owner can view archived items."
            )
            : null;
    }


    private static IQueryable<NotebookItemRow> BuildItemRowQuery(
        IQueryable<NotebookItem> query,
        bool includeContent
    )
    {
        return includeContent
            ? query.Select(item => new NotebookItemRow(
                item.Id,
                item.NotebookId,
                item.ParentId,
                item.Type,
                item.Title,
                item.Slug,
                item.Path,
                item.SortOrder,
                item.ContentJson,
                item.PlainTextContent,
                item.IsArchived,
                item.ArchivedAtUtc,
                item.ArchivedByUserId,
                item.CreatedAtUtc,
                item.UpdatedAtUtc
            ))
            : query.Select(item => new NotebookItemRow(
                item.Id,
                item.NotebookId,
                item.ParentId,
                item.Type,
                item.Title,
                item.Slug,
                item.Path,
                item.SortOrder,
                null,
                null,
                item.IsArchived,
                item.ArchivedAtUtc,
                item.ArchivedByUserId,
                item.CreatedAtUtc,
                item.UpdatedAtUtc
            ));
    }

    private static IOrderedQueryable<NotebookItem> OrderNotebookItems(
        IQueryable<NotebookItem> query
    )
    {
        return query
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title);
    }

    private static bool TryParseItemType(string? type, out NotebookItemType itemType)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            itemType = default;
            return false;
        }

        return Enum.TryParse(type, ignoreCase: true, out itemType) && Enum.IsDefined(itemType);
    }

    private static IQueryable<T> ApplyLimit<T>(IQueryable<T> query, int? limit)
    {
        return limit.HasValue ? query.Take(Math.Max(1, limit.Value)) : query;
    }

    private static IQueryable<T> ApplyPage<T>(IQueryable<T> query, int? offset, int? limit)
    {
        var normalizedOffset = Math.Max(0, offset ?? 0);
        if (normalizedOffset > 0)
        {
            query = query.Skip(normalizedOffset);
        }

        return ApplyLimit(query, limit);
    }

    private static IReadOnlyList<T> ApplyPage<T>(IReadOnlyList<T> values, int? offset, int? limit)
    {
        var normalizedOffset = Math.Max(0, offset ?? 0);
        IEnumerable<T> result = values.Skip(normalizedOffset);
        if (limit.HasValue)
        {
            result = result.Take(Math.Max(1, limit.Value));
        }

        return result.ToList();
    }

    private sealed record NotebookAccessRow(
        Guid Id,
        Guid OwnerId,
        NotebookVisibility Visibility,
        string? AccessCodeHash,
        bool IsSharedWithUser
    );
}
