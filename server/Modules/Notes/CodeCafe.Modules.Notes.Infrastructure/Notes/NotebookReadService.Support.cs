using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Domain.Notes;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Modules.Notes.Infrastructure.Notes;

public sealed partial class NotebookReadService
{
    private async Task<Notebook?> GetPublicNotebookEntityBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.AsNoTracking().SingleOrDefaultAsync(
            notebook => notebook.Slug == slug
                && notebook.Visibility == NotebookVisibility.Public
                && notebook.IsPublished,
            cancellationToken);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetPublicReadableNotebookAccessAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook =>
                existingNotebook.Slug == slug
                && existingNotebook.Visibility == NotebookVisibility.Public
                && existingNotebook.IsPublished)
            .Select(existingNotebook => new NotebookAccessRow(existingNotebook.Id, existingNotebook.OwnerId, existingNotebook.Visibility, existingNotebook.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetReadableNotebookAccessAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookId)
            .Select(existingNotebook => new NotebookAccessRow(existingNotebook.Id, existingNotebook.OwnerId, existingNotebook.Visibility, existingNotebook.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetReadableNotebookAccessAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Slug == slug)
            .Select(existingNotebook => new NotebookAccessRow(existingNotebook.Id, existingNotebook.OwnerId, existingNotebook.Visibility, existingNotebook.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId);
    }

    private static NotesResult<NotebookAccessRow> ToReadableNotebookAccessResult(NotebookAccessRow? notebook, Guid currentUserId)
    {
        if (notebook is null)
        {
            return NotesResult<NotebookAccessRow>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        return NotebookAccessPolicy.CanReadNotebook(notebook.OwnerId, notebook.Visibility, notebook.IsPublished, currentUserId)
            ? NotesResult<NotebookAccessRow>.Success(notebook)
            : NotesResult<NotebookAccessRow>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
    }

    private static NotesError? GetArchivedReadError(
        NotebookAccessRow notebook,
        Guid currentUserId,
        bool includeArchived)
    {
        return includeArchived && notebook.OwnerId != currentUserId
            ? new NotesError(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "Only the notebook owner can view archived items.")
            : null;
    }

    private bool UsesPostgresProvider()
    {
        return string.Equals(dbContext.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);
    }

    private static IQueryable<NotebookItemRow> BuildItemRowQuery(IQueryable<NotebookItem> query, bool includeContent)
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
                item.ContentFormat,
                item.ContentJson,
                item.PlainTextContent,
                item.IsArchived,
                item.ArchivedAtUtc,
                item.ArchivedByUserId,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
            : query.Select(item => new NotebookItemRow(
                item.Id,
                item.NotebookId,
                item.ParentId,
                item.Type,
                item.Title,
                item.Slug,
                item.Path,
                item.SortOrder,
                item.ContentFormat,
                null,
                null,
                item.IsArchived,
                item.ArchivedAtUtc,
                item.ArchivedByUserId,
                item.CreatedAtUtc,
                item.UpdatedAtUtc));
    }

    private static IOrderedQueryable<NotebookItem> OrderNotebookItems(IQueryable<NotebookItem> query)
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
        return limit.HasValue
            ? query.Take(Math.Max(1, limit.Value))
            : query;
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
        bool IsPublished);
}
