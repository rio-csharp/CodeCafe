namespace CodeCafe.Application.Notes;

public interface INotebookReadService
{
    Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit = null,
        int? offset = null
    );

    Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null,
        int? offset = null
    );

    Task<IReadOnlyList<NotebookSummaryModel>> GetFavoriteNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null,
        int? offset = null
    );

    Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
        Guid currentUserId,
        string search,
        CancellationToken cancellationToken,
        int? limit = null
    );

    Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = false
    );

    Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(
        string slug,
        CancellationToken cancellationToken
    );

    Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(
        string slug,
        string path,
        CancellationToken cancellationToken
    );

    Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = false
    );

    Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = false,
        string? accessCode = null
    );

    Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeContent = false,
        int? limit = null
    );

    Task<NotesResult<NotebookItemModel>> GetNotebookItemByIdAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false
    );

    Task<NotesResult<NotebookFavoriteModel>> GetNotebookFavoriteStatusAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken
    );

    private static NotesResult<T> Failure<T>(NotesError error) =>
        NotesResult<T>.Failure(error.Kind, error.Code, error.Message);

    async Task<NotesResult<NotebookSummaryModel>> GetNotebookSummaryBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false
    )
    {
        var notebookResult = await GetNotebookBySlugAsync(
            slug,
            currentUserId,
            cancellationToken,
            includeArchived,
            includeItems: false
        );
        if (!notebookResult.Succeeded)
        {
            return Failure<NotebookSummaryModel>(notebookResult.Error!);
        }

        var notebook = notebookResult.Value!;
        return NotesResult<NotebookSummaryModel>.Success(
            new NotebookSummaryModel(
                notebook.Id,
                notebook.OwnerId,
                notebook.Title,
                notebook.Slug,
                notebook.Description,
                notebook.Visibility,
                notebook.AuthorDisplayName,
                notebook.CanEdit,
                notebook.ItemCount,
                notebook.FolderCount,
                notebook.PageCount,
                notebook.FavoriteCount,
                notebook.IsFavoritedByMe,
                notebook.LastActivityAtUtc,
                notebook.CreatedAtUtc,
                notebook.UpdatedAtUtc,
                notebook.PublishedAtUtc
            )
        );
    }

    async Task<NotesResult<NotebookItemModel>> GetNotebookItemByPathAsync(
        string notebookSlug,
        string path,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false
    )
    {
        var notebookResult = await GetNotebookBySlugAsync(
            notebookSlug,
            currentUserId,
            cancellationToken,
            includeArchived,
            includeItems: true
        );
        if (!notebookResult.Succeeded)
        {
            return Failure<NotebookItemModel>(notebookResult.Error!);
        }

        var normalizedPath = NotebookInput.NormalizePath(path);
        var item = notebookResult.Value!.Items.SingleOrDefault(existingItem =>
            string.Equals(existingItem.Path, normalizedPath, StringComparison.Ordinal)
        );
        return item is null
            ? NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            )
            : NotesResult<NotebookItemModel>.Success(item);
    }

    // Loads the notebook once and returns both the AI context projection and the full item for
    // activePagePath, so callers needing both avoid loading the whole notebook (including every
    // page's content) twice per request. A null activePagePath yields a null item with
    // ActivePageResolved = true; any non-null value is looked up. Only "page" items can match.
    async Task<NotesResult<NotebookContextWithItem>> GetNotebookContextWithItemAsync(
        string slug,
        string? activePagePath,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        var notebookResult = await GetNotebookBySlugAsync(
            slug,
            currentUserId,
            cancellationToken,
            includeArchived: false,
            includeItems: true
        );
        if (!notebookResult.Succeeded)
        {
            return Failure<NotebookContextWithItem>(notebookResult.Error!);
        }

        var context = BuildContext(notebookResult.Value!);
        if (activePagePath is null)
        {
            return NotesResult<NotebookContextWithItem>.Success(
                new NotebookContextWithItem(context, null, ActivePageResolved: true)
            );
        }

        var normalizedPath = NotebookInput.NormalizePath(activePagePath);
        var activePage = notebookResult.Value!.Items.SingleOrDefault(existingItem =>
            string.Equals(existingItem.Path, normalizedPath, StringComparison.Ordinal)
            && string.Equals(existingItem.Type, "page", StringComparison.OrdinalIgnoreCase)
        );

        return NotesResult<NotebookContextWithItem>.Success(
            new NotebookContextWithItem(context, activePage, activePage is not null)
        );
    }

    async Task<NotesResult<NotebookContextModel>> GetNotebookContextAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        var notebookResult = await GetNotebookBySlugAsync(
            slug,
            currentUserId,
            cancellationToken,
            includeArchived: false,
            includeItems: true
        );
        if (!notebookResult.Succeeded)
        {
            return Failure<NotebookContextModel>(notebookResult.Error!);
        }

        return NotesResult<NotebookContextModel>.Success(BuildContext(notebookResult.Value!));
    }

    private static NotebookContextModel BuildContext(NotebookDetailModel notebook)
    {
        var items = notebook
            .Items.Select(item => new NotebookContextItemModel(
                item.Id,
                item.ParentId,
                item.Type,
                item.Title,
                item.Path,
                item.SortOrder,
                TruncateTextPreview(item.PlainTextContent)
            ))
            .ToList();

        return new NotebookContextModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.CanEdit,
            items
        );
    }

    private static string? TruncateTextPreview(string? value)
    {
        return value is not null && value.Length > NotebookContextModel.TextPreviewChars
            ? value[..NotebookContextModel.TextPreviewChars]
            : value;
    }

    async Task<NotesResult<NotebookItemsPageModel>> GetNotebookItemsPageAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        Guid? parentId = null,
        string? type = null,
        int? offset = null,
        int? limit = null
    )
    {
        var itemsResult = await GetNotebookItemsAsync(
            notebookId,
            currentUserId,
            search,
            cancellationToken,
            includeArchived,
            includeContent: false
        );
        if (!itemsResult.Succeeded)
        {
            return NotesResult<NotebookItemsPageModel>.Failure(
                itemsResult.Error!.Kind,
                itemsResult.Error.Code,
                itemsResult.Error.Message
            );
        }

        var filteredItems = itemsResult
            .Value!.Where(item => parentId is null || item.ParentId == parentId)
            .Where(item =>
                string.IsNullOrWhiteSpace(type)
                || string.Equals(item.Type, type, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var normalizedOffset = Math.Max(0, offset ?? 0);
        var normalizedLimit = limit.HasValue ? Math.Max(1, limit.Value) : filteredItems.Count;
        var pagedItems = filteredItems.Skip(normalizedOffset).Take(normalizedLimit).ToList();

        return NotesResult<NotebookItemsPageModel>.Success(
            new NotebookItemsPageModel(filteredItems.Count, pagedItems)
        );
    }
}
