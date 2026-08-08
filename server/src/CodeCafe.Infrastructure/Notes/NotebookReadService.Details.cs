using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes;

public sealed partial class NotebookReadService
{
    public async Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
    {
        var notebookAccessResult = await GetPublicReadableNotebookAccessAsync(slug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .SingleAsync(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id, cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken, includeArchived, includeItems, includeContent));
    }

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(notebookId, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .SingleAsync(existingNotebook => existingNotebook.Id == notebookId, cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken, includeArchived, includeItems, includeContent));
    }

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(slug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .SingleAsync(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id, cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken, includeArchived, includeItems, includeContent));
    }

    public async Task<NotesResult<NotebookContextModel>> GetNotebookContextAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(slug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookContextModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        var access = notebookAccessResult.Value!;
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == access.Id)
            .Select(existingNotebook => new
            {
                existingNotebook.Id,
                existingNotebook.OwnerId,
                existingNotebook.Title,
                existingNotebook.Slug,
                existingNotebook.Description
            })
            .SingleAsync(cancellationToken);

        var items = await OrderNotebookItems(dbContext.NotebookItems
                .AsNoTracking()
                .Where(item => item.NotebookId == access.Id && !item.IsArchived))
            .Select(item => new NotebookContextItemModel(
                item.Id,
                item.ParentId,
                item.Type.ToString().ToLowerInvariant(),
                item.Title,
                item.Path,
                item.SortOrder,
                item.PlainTextContent == null
                    ? null
                    : item.PlainTextContent.Substring(0, NotebookContextModel.TextPreviewChars)))
            .ToListAsync(cancellationToken);

        return NotesResult<NotebookContextModel>.Success(new NotebookContextModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.OwnerId == currentUserId,
            items));
    }

    public async Task<NotesResult<NotebookSummaryModel>> GetNotebookSummaryBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(slug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookSummaryModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookSummaryModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        if (!UsesPostgresProvider())
        {
            var notebook = await dbContext.Notebooks
                .AsNoTracking()
                .SingleAsync(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id, cancellationToken);
            var summaries = await ToSummaryModelsAsync([notebook], currentUserId, cancellationToken, includeArchived);
            return NotesResult<NotebookSummaryModel>.Success(summaries.Single());
        }

        var summary = await BuildNotebookSummaryRows(
                dbContext.Notebooks
                    .AsNoTracking()
                    .Where(notebook => notebook.Id == notebookAccessResult.Value!.Id),
                currentUserId,
                includeArchived)
            .SingleAsync(cancellationToken);

        return NotesResult<NotebookSummaryModel>.Success(ToSummaryModel(summary));
    }

    private async Task<NotebookDetailModel> ToDetailModelAsync(
        Notebook notebook,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived,
        bool includeItems,
        bool includeContent)
    {
        var authorDisplayName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == notebook.OwnerId)
            .Select(user => user.DisplayName)
            .SingleOrDefaultAsync(cancellationToken)
            ?? "Unknown";

        IReadOnlyList<NotebookItemModel> items = [];
        NotebookMetadata metadata;
        if (includeItems)
        {
            items = await LoadNotebookItemModelsAsync(notebook.Id, includeArchived, includeContent, cancellationToken);
            metadata = await BuildLoadedDetailMetadataAsync(notebook, items, currentUserId, cancellationToken);
        }
        else
        {
            metadata = (await GetNotebookMetadataAsync([notebook], currentUserId, cancellationToken, includeArchived)).GetValueOrDefault(notebook.Id) ?? NotebookMetadata.Empty;
        }

        return NotesSupport.ToDetailModel(notebook, authorDisplayName, metadata, currentUserId, items);
    }

    private async Task<IReadOnlyList<NotebookItemModel>> LoadNotebookItemModelsAsync(
        Guid notebookId,
        bool includeArchived,
        bool includeContent,
        CancellationToken cancellationToken)
    {
        var rows = await BuildItemRowQuery(
                OrderNotebookItems(dbContext.NotebookItems
                    .AsNoTracking()
                    .Where(item => item.NotebookId == notebookId && (includeArchived || !item.IsArchived))),
                includeContent)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => NotesSupport.ToItemModel(row, includeContent))
            .ToList();
    }

    private async Task<NotebookMetadata> BuildLoadedDetailMetadataAsync(
        Notebook notebook,
        IReadOnlyList<NotebookItemModel> items,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var favoriteModel = await BuildFavoriteModelAsync(notebook.Id, currentUserId, cancellationToken);
        var lastActivityAtUtc = new[]
        {
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc
        }
        .Concat(items.Select(item => item.UpdatedAtUtc ?? item.CreatedAtUtc))
        .Max();

        return new NotebookMetadata(
            ItemCount: items.Count,
            FolderCount: items.Count(item => string.Equals(item.Type, "folder", StringComparison.OrdinalIgnoreCase)),
            PageCount: items.Count(item => string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase)),
            FavoriteCount: favoriteModel.FavoriteCount,
            IsFavoritedByMe: favoriteModel.IsFavorited,
            LastActivityAtUtc: lastActivityAtUtc);
    }

    private async Task<NotebookFavoriteModel> BuildFavoriteModelAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var favoriteCount = await dbContext.NotebookFavorites
            .AsNoTracking()
            .CountAsync(favorite => favorite.NotebookId == notebookId, cancellationToken);

        var isFavorited = currentUserId != Guid.Empty
            && await dbContext.NotebookFavorites
                .AsNoTracking()
                .AnyAsync(
                    favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId,
                    cancellationToken);

        return new NotebookFavoriteModel(notebookId, isFavorited, favoriteCount);
    }
}
