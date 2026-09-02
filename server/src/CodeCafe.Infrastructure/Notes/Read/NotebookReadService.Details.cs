using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Read;

public sealed partial class NotebookReadService
{
    public async Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = false
    )
    {
        var notebookAccessResult = await GetPublicReadableNotebookAccessAsync(
            slug,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookDetailModel>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<NotebookDetailModel>(archiveError);
        }

        var notebookRow = await dbContext
            .Notebooks.AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id)
            .Select(existingNotebook => new
            {
                Notebook = existingNotebook,
                AuthorDisplayName = dbContext
                    .Users.Where(user => user.Id == existingNotebook.OwnerId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
            })
            .SingleAsync(cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(
            await ToDetailModelAsync(
                notebookRow.Notebook,
                notebookRow.AuthorDisplayName,
                currentUserId,
                cancellationToken,
                includeArchived,
                includeItems,
                includeContent
            )
        );
    }

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = false
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            notebookId,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookDetailModel>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<NotebookDetailModel>(archiveError);
        }

        var notebookRow = await dbContext
            .Notebooks.AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookId)
            .Select(existingNotebook => new
            {
                Notebook = existingNotebook,
                AuthorDisplayName = dbContext
                    .Users.Where(user => user.Id == existingNotebook.OwnerId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
            })
            .SingleAsync(cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(
            await ToDetailModelAsync(
                notebookRow.Notebook,
                notebookRow.AuthorDisplayName,
                currentUserId,
                cancellationToken,
                includeArchived,
                includeItems,
                includeContent
            )
        );
    }

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = false,
        string? accessCode = null
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            slug,
            currentUserId,
            cancellationToken,
            accessCode
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookDetailModel>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<NotebookDetailModel>(archiveError);
        }

        var notebookRow = await dbContext
            .Notebooks.AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id)
            .Select(existingNotebook => new
            {
                Notebook = existingNotebook,
                AuthorDisplayName = dbContext
                    .Users.Where(user => user.Id == existingNotebook.OwnerId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
            })
            .SingleAsync(cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(
            await ToDetailModelAsync(
                notebookRow.Notebook,
                notebookRow.AuthorDisplayName,
                currentUserId,
                cancellationToken,
                includeArchived,
                includeItems,
                includeContent
            )
        );
    }

    public async Task<NotesResult<NotebookContextModel>> GetNotebookContextAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            slug,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookContextModel>(notebookAccessResult.Error!);
        }

        var access = notebookAccessResult.Value!;
        var notebook = await dbContext
            .Notebooks.AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == access.Id)
            .Select(existingNotebook => new
            {
                existingNotebook.Id,
                existingNotebook.OwnerId,
                existingNotebook.Title,
                existingNotebook.Slug,
                existingNotebook.Description,
            })
            .SingleAsync(cancellationToken);

        var itemRows = await OrderNotebookItems(
                dbContext
                    .NotebookItems.AsNoTracking()
                    .Where(item => item.NotebookId == access.Id && !item.IsArchived)
            )
            .Select(item => new
            {
                item.Id,
                item.ParentId,
                item.Type,
                item.Title,
                item.Path,
                item.SortOrder,
                TextPreview =
                    item.PlainTextContent == null
                        ? null
                        : item.PlainTextContent.Substring(0, NotebookContextModel.TextPreviewChars),
            })
            .ToListAsync(cancellationToken);

        var items = itemRows
            .Select(row => new NotebookContextItemModel(
                row.Id,
                row.ParentId,
                row.Type.ToString().ToLowerInvariant(),
                row.Title,
                row.Path.Value,
                row.SortOrder,
                row.TextPreview
            ))
            .ToList();

        return NotesResult<NotebookContextModel>.Success(
            new NotebookContextModel(
                notebook.Id,
                notebook.OwnerId,
                notebook.Title,
                notebook.Slug.Value,
                notebook.Description,
                notebook.OwnerId == currentUserId,
                items
            )
        );
    }

    public async Task<NotesResult<NotebookSummaryModel>> GetNotebookSummaryBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            slug,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookSummaryModel>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<NotebookSummaryModel>(archiveError);
        }

        var summary = await BuildNotebookSummaryRows(
                dbContext
                    .Notebooks.AsNoTracking()
                    .Where(notebook => notebook.Id == notebookAccessResult.Value!.Id),
                currentUserId,
                includeArchived
            )
            .SingleAsync(cancellationToken);

        return NotesResult<NotebookSummaryModel>.Success(ToSummaryModel(summary));
    }

    private async Task<NotebookDetailModel> ToDetailModelAsync(
        Notebook notebook,
        string? authorDisplayName,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived,
        bool includeItems,
        bool includeContent
    )
    {
        authorDisplayName ??= "Unknown";

        IReadOnlyList<NotebookItemModel> items = [];
        NotebookMetadata metadata;
        if (includeItems)
        {
            items = await LoadNotebookItemModelsAsync(
                notebook.Id,
                includeArchived,
                includeContent,
                cancellationToken
            );
            metadata = await BuildLoadedDetailMetadataAsync(
                notebook,
                items,
                currentUserId,
                cancellationToken
            );
        }
        else
        {
            metadata =
                (
                    await GetNotebookMetadataAsync(
                        [notebook],
                        currentUserId,
                        cancellationToken,
                        includeArchived
                    )
                ).GetValueOrDefault(notebook.Id) ?? NotebookMetadata.Empty;
        }

        return NotesSupport.ToDetailModel(
            notebook,
            authorDisplayName,
            metadata,
            currentUserId,
            items
        );
    }

    private async Task<IReadOnlyList<NotebookItemModel>> LoadNotebookItemModelsAsync(
        Guid notebookId,
        bool includeArchived,
        bool includeContent,
        CancellationToken cancellationToken
    )
    {
        var rows = await BuildItemRowQuery(
                OrderNotebookItems(
                    dbContext
                        .NotebookItems.AsNoTracking()
                        .Where(item =>
                            item.NotebookId == notebookId && (includeArchived || !item.IsArchived)
                        )
                ),
                includeContent
            )
            .ToListAsync(cancellationToken);

        return rows.Select(row => NotesSupport.ToItemModel(row, includeContent)).ToList();
    }

    private async Task<NotebookMetadata> BuildLoadedDetailMetadataAsync(
        Notebook notebook,
        IReadOnlyList<NotebookItemModel> items,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        var favoriteModel = await BuildFavoriteModelAsync(
            notebook.Id,
            currentUserId,
            cancellationToken
        );
        var lastActivityAtUtc = new[]
        {
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc,
        }
            .Concat(items.Select(item => item.UpdatedAtUtc ?? item.CreatedAtUtc))
            .Max();

        return new NotebookMetadata(
            ItemCount: items.Count,
            FolderCount: items.Count(item =>
                string.Equals(item.Type, "folder", StringComparison.OrdinalIgnoreCase)
            ),
            PageCount: items.Count(item =>
                string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase)
            ),
            FavoriteCount: favoriteModel.FavoriteCount,
            IsFavoritedByMe: favoriteModel.IsFavorited,
            LastActivityAtUtc: lastActivityAtUtc
        );
    }

    private async Task<NotebookFavoriteModel> BuildFavoriteModelAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        var stats = await dbContext
            .NotebookFavorites.AsNoTracking()
            .Where(favorite => favorite.NotebookId == notebookId)
            .GroupBy(favorite => favorite.NotebookId)
            .Select(group => new
            {
                Count = group.Count(),
                IsFavorited =
                    currentUserId != Guid.Empty
                    && group.Any(favorite => favorite.UserId == currentUserId),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new NotebookFavoriteModel(
            notebookId,
            stats?.IsFavorited ?? false,
            stats?.Count ?? 0
        );
    }

    private static NotesResult<T> Failure<T>(NotesError error) =>
        NotesResult<T>.Failure(error.Kind, error.Code, error.Message);
}
