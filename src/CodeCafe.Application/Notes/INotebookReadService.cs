namespace CodeCafe.Application.Notes;

public interface INotebookReadService
{
    Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit = null);

    Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null);

    Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
        Guid currentUserId,
        string search,
        CancellationToken cancellationToken,
        int? limit = null);

    Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true);

    Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken);

    Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken);

    Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true);

    Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true);

    Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        int? limit = null);

    async Task<NotesResult<NotebookSummaryModel>> GetNotebookSummaryBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebookResult = await GetNotebookBySlugAsync(
            slug,
            currentUserId,
            cancellationToken,
            includeArchived,
            includeItems: false);
        if (!notebookResult.Succeeded)
        {
            return NotesResult<NotebookSummaryModel>.Failure(
                notebookResult.Error!.Kind,
                notebookResult.Error.Code,
                notebookResult.Error.Message);
        }

        var notebook = notebookResult.Value!;
        return NotesResult<NotebookSummaryModel>.Success(new NotebookSummaryModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.Visibility,
            notebook.IsPublished,
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
            notebook.PublishedAtUtc));
    }

    async Task<NotesResult<NotebookItemModel>> GetNotebookItemByPathAsync(
        string notebookSlug,
        string path,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebookResult = await GetNotebookBySlugAsync(
            notebookSlug,
            currentUserId,
            cancellationToken,
            includeArchived,
            includeItems: true);
        if (!notebookResult.Succeeded)
        {
            return NotesResult<NotebookItemModel>.Failure(
                notebookResult.Error!.Kind,
                notebookResult.Error.Code,
                notebookResult.Error.Message);
        }

        var normalizedPath = NotebookInput.NormalizePath(path);
        var item = notebookResult.Value!.Items.SingleOrDefault(existingItem =>
            string.Equals(existingItem.Path, normalizedPath, StringComparison.Ordinal));
        return item is null
            ? NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found.")
            : NotesResult<NotebookItemModel>.Success(item);
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
        int? limit = null)
    {
        var itemsResult = await GetNotebookItemsAsync(
            notebookId,
            currentUserId,
            search,
            cancellationToken,
            includeArchived);
        if (!itemsResult.Succeeded)
        {
            return NotesResult<NotebookItemsPageModel>.Failure(
                itemsResult.Error!.Kind,
                itemsResult.Error.Code,
                itemsResult.Error.Message);
        }

        var filteredItems = itemsResult.Value!
            .Where(item => parentId is null || item.ParentId == parentId)
            .Where(item => string.IsNullOrWhiteSpace(type) || string.Equals(item.Type, type, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var normalizedOffset = Math.Max(0, offset ?? 0);
        var normalizedLimit = limit.HasValue ? Math.Max(1, limit.Value) : filteredItems.Count;
        var pagedItems = filteredItems
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .ToList();

        return NotesResult<NotebookItemsPageModel>.Success(new NotebookItemsPageModel(filteredItems.Count, pagedItems));
    }
}
