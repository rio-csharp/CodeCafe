using System.ComponentModel;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Common.Identity;
using CodeCafe.Application.Notes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Ai;

public sealed class NotebookAssistantTools(
    IServiceScopeFactory serviceScopeFactory,
    ICurrentUserAccessor currentUserAccessor,
    IOptions<AiOptions> aiOptionsAccessor
)
{
    private readonly AiOptions _options = aiOptionsAccessor.Value;

    [Description(
        "List notebooks the current user can access, optionally filtered by a search query."
    )]
    public async Task<ListNotebooksToolResponse> ListNotebooksAsync(
        [Description("Optional notebook title or description search query.")] string? query = null,
        [Description("Maximum number of notebooks to return.")] int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetCurrentUserId(out var currentUserId, out var authError))
        {
            return ListNotebooksToolResponse.Failure(authError);
        }

        var normalizedLimit = NormalizeLimit(limit);
        using var scope = serviceScopeFactory.CreateScope();
        var notebookReadService = scope.ServiceProvider.GetRequiredService<INotebookReadService>();
        var ownedNotebooks = await notebookReadService.GetMyNotebooksAsync(
            currentUserId,
            query,
            cancellationToken,
            normalizedLimit
        );
        var publicNotebooks = await notebookReadService.GetPublicNotebooksAsync(
            query,
            currentUserId,
            cancellationToken,
            normalizedLimit
        );

        return ListNotebooksToolResponse.Success(
            ownedNotebooks
                .Concat(publicNotebooks)
                .GroupBy(notebook => notebook.Id)
                .Select(group => group.First())
                .OrderByDescending(notebook => notebook.LastActivityAtUtc)
                .ThenBy(notebook => notebook.Title, StringComparer.OrdinalIgnoreCase)
                .Take(normalizedLimit)
                .Select(ToNotebookSummary)
                .ToList()
        );
    }

    [Description("Search notebook items visible to the current user.")]
    public async Task<SearchNotesToolResponse> SearchNotesAsync(
        [Description("Required search query for page titles or plain text content.")] string query,
        [Description("Maximum number of search results to return.")] int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetCurrentUserId(out var currentUserId, out var authError))
        {
            return SearchNotesToolResponse.Failure(authError);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return SearchNotesToolResponse.Failure(
                new NotebookAssistantToolError("invalid_search", "Search query is required.")
            );
        }

        using var scope = serviceScopeFactory.CreateScope();
        var notebookReadService = scope.ServiceProvider.GetRequiredService<INotebookReadService>();
        var results = await notebookReadService.SearchVisibleNotebookItemsAsync(
            currentUserId,
            query,
            cancellationToken,
            NormalizeLimit(limit)
        );

        return SearchNotesToolResponse.Success(results.Select(ToSearchResult).ToList());
    }

    [Description("Load notebook metadata and item summaries by notebook slug.")]
    public async Task<GetNotebookToolResponse> GetNotebookAsync(
        [Description("Notebook slug.")] string slug,
        [Description("Whether to include item summaries.")] bool includeItems = true,
        [Description("Maximum number of item summaries to return.")] int? itemLimit = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetCurrentUserId(out var currentUserId, out var authError))
        {
            return GetNotebookToolResponse.Failure(authError);
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return GetNotebookToolResponse.Failure(
                new NotebookAssistantToolError("invalid_slug", "Notebook slug is required.")
            );
        }

        using var scope = serviceScopeFactory.CreateScope();
        var notebookReadService = scope.ServiceProvider.GetRequiredService<INotebookReadService>();
        var result = await notebookReadService.GetNotebookBySlugAsync(
            slug,
            currentUserId,
            cancellationToken,
            includeItems: includeItems
        );

        if (!result.Succeeded)
        {
            return GetNotebookToolResponse.Failure(ToToolError(result.Error!));
        }

        var notebook = result.Value!;
        return GetNotebookToolResponse.Success(
            new NotebookDetailForAi(
                ToNotebookSummary(notebook),
                includeItems
                    ? notebook.Items.Take(NormalizeLimit(itemLimit)).Select(ToNotebookItem).ToList()
                    : []
            )
        );
    }

    [Description("Load one visible notebook page or folder by notebook slug and item path.")]
    public async Task<GetPageToolResponse> GetPageAsync(
        [Description("Notebook slug.")] string notebookSlug,
        [Description("Item path inside the notebook.")] string path,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryGetCurrentUserId(out var currentUserId, out var authError))
        {
            return GetPageToolResponse.Failure(authError);
        }

        if (string.IsNullOrWhiteSpace(notebookSlug) || string.IsNullOrWhiteSpace(path))
        {
            return GetPageToolResponse.Failure(
                new NotebookAssistantToolError(
                    "invalid_page_reference",
                    "Notebook slug and page path are required."
                )
            );
        }

        using var scope = serviceScopeFactory.CreateScope();
        var notebookReadService = scope.ServiceProvider.GetRequiredService<INotebookReadService>();
        var result = await notebookReadService.GetNotebookItemByPathAsync(
            notebookSlug,
            path,
            currentUserId,
            cancellationToken
        );

        if (!result.Succeeded)
        {
            return GetPageToolResponse.Failure(ToToolError(result.Error!));
        }

        var item = result.Value!;
        if (!string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase))
        {
            return GetPageToolResponse.Failure(
                new NotebookAssistantToolError(
                    "notebook_item_not_found",
                    $"The item at path '{path}' is not a page."
                )
            );
        }

        return GetPageToolResponse.Success(ToNotebookItem(item));
    }

    private bool TryGetCurrentUserId(out Guid currentUserId, out NotebookAssistantToolError error)
    {
        if (currentUserAccessor.GetCurrentUserId() is Guid userId)
        {
            currentUserId = userId;
            error = default!;
            return true;
        }

        currentUserId = Guid.Empty;
        error = new NotebookAssistantToolError(
            "authentication_required",
            "Authentication is required to read notebooks."
        );
        return false;
    }

    private int NormalizeLimit(int? limit)
    {
        var max = Math.Max(1, _options.MaxToolResults);
        return Math.Clamp(limit ?? max, 1, max);
    }

    private NotebookSummaryForAi ToNotebookSummary(NotebookSummaryModel notebook)
    {
        return new NotebookSummaryForAi(
            notebook.Title,
            notebook.Slug,
            Truncate(notebook.Description),
            notebook.Visibility,
            notebook.IsPublished,
            notebook.AuthorDisplayName,
            notebook.CanEdit,
            notebook.ItemCount,
            notebook.FolderCount,
            notebook.PageCount,
            notebook.LastActivityAtUtc,
            notebook.PublishedAtUtc
        );
    }

    private NotebookSummaryForAi ToNotebookSummary(NotebookDetailModel notebook)
    {
        return new NotebookSummaryForAi(
            notebook.Title,
            notebook.Slug,
            Truncate(notebook.Description),
            notebook.Visibility,
            notebook.IsPublished,
            notebook.AuthorDisplayName,
            notebook.CanEdit,
            notebook.ItemCount,
            notebook.FolderCount,
            notebook.PageCount,
            notebook.LastActivityAtUtc,
            notebook.PublishedAtUtc
        );
    }

    private NotebookItemForAi ToNotebookItem(NotebookItemModel item)
    {
        return new NotebookItemForAi(
            item.Type,
            item.Title,
            item.Path,
            item.SortOrder,
            item.ContentFormat,
            Truncate(item.PlainTextContent),
            item.CreatedAtUtc,
            item.UpdatedAtUtc
        );
    }

    private NotebookSearchResultForAi ToSearchResult(NotebookItemSearchModel item)
    {
        return new NotebookSearchResultForAi(
            item.NotebookTitle,
            item.NotebookSlug,
            item.NotebookCanEdit,
            item.Title,
            item.Type,
            item.Path,
            Truncate(item.PlainTextContent),
            item.CreatedAtUtc,
            item.UpdatedAtUtc
        );
    }

    private string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var maxLength = Math.Max(1, _options.MaxToolContentChars);
        return value.Length <= maxLength
            ? value
            : string.Concat(value.AsSpan(0, maxLength), "\n[truncated]");
    }

    private static NotebookAssistantToolError ToToolError(NotesError error) =>
        new(error.Code, error.Message);
}

public sealed record NotebookAssistantToolError(string Code, string Message);

public sealed record ListNotebooksToolResponse(
    bool Succeeded,
    NotebookAssistantToolError? Error,
    int TotalCount,
    IReadOnlyList<NotebookSummaryForAi> Notebooks
)
{
    public static ListNotebooksToolResponse Success(
        IReadOnlyList<NotebookSummaryForAi> notebooks
    ) => new(true, null, notebooks.Count, notebooks);

    public static ListNotebooksToolResponse Failure(NotebookAssistantToolError error) =>
        new(false, error, 0, []);
}

public sealed record SearchNotesToolResponse(
    bool Succeeded,
    NotebookAssistantToolError? Error,
    int TotalCount,
    IReadOnlyList<NotebookSearchResultForAi> Results
)
{
    public static SearchNotesToolResponse Success(
        IReadOnlyList<NotebookSearchResultForAi> results
    ) => new(true, null, results.Count, results);

    public static SearchNotesToolResponse Failure(NotebookAssistantToolError error) =>
        new(false, error, 0, []);
}

public sealed record GetNotebookToolResponse(
    bool Succeeded,
    NotebookAssistantToolError? Error,
    NotebookDetailForAi? Notebook
)
{
    public static GetNotebookToolResponse Success(NotebookDetailForAi notebook) =>
        new(true, null, notebook);

    public static GetNotebookToolResponse Failure(NotebookAssistantToolError error) =>
        new(false, error, null);
}

public sealed record GetPageToolResponse(
    bool Succeeded,
    NotebookAssistantToolError? Error,
    NotebookItemForAi? Item
)
{
    public static GetPageToolResponse Success(NotebookItemForAi item) => new(true, null, item);

    public static GetPageToolResponse Failure(NotebookAssistantToolError error) =>
        new(false, error, null);
}

public sealed record NotebookSummaryForAi(
    string Title,
    string Slug,
    string? Description,
    string Visibility,
    bool IsPublished,
    string AuthorDisplayName,
    bool CanEdit,
    int ItemCount,
    int FolderCount,
    int PageCount,
    DateTimeOffset LastActivityAtUtc,
    DateTimeOffset? PublishedAtUtc
);

public sealed record NotebookDetailForAi(
    NotebookSummaryForAi Summary,
    IReadOnlyList<NotebookItemForAi> Items
);

public sealed record NotebookItemForAi(
    string Type,
    string Title,
    string Path,
    int SortOrder,
    string? ContentFormat,
    string? PlainTextContent,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);

public sealed record NotebookSearchResultForAi(
    string NotebookTitle,
    string NotebookSlug,
    bool NotebookCanEdit,
    string Title,
    string Type,
    string Path,
    string? PlainTextSnippet,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);
