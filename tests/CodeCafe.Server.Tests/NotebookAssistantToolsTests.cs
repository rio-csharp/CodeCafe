using CodeCafe.Ai.Configuration;
using CodeCafe.Ai.Tools;
using CodeCafe.Application.Notes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Xunit;

namespace CodeCafe.Server.Tests;

public sealed class NotebookAssistantToolsTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SharedNotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PrivateNotebookId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PublicNotebookId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task ListNotebooksAsync_ReturnsOwnedAndPublicNotebooksWithoutDuplicatingOwnedPublicNotebook()
    {
        var readService = new FakeNotebookReadService
        {
            MyNotebooks =
            [
                CreateNotebookSummary(
                    SharedNotebookId,
                    "Architecture Notes",
                    "architecture-notes",
                    canEdit: true,
                    lastActivityAtUtc: DateTimeOffset.Parse("2026-06-02T00:00:00+00:00")),
                CreateNotebookSummary(
                    PrivateNotebookId,
                    "Private Drafts",
                    "private-drafts",
                    canEdit: true,
                    lastActivityAtUtc: DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
            ],
            PublicNotebooks =
            [
                CreateNotebookSummary(
                    SharedNotebookId,
                    "Architecture Notes",
                    "architecture-notes",
                    canEdit: false,
                    lastActivityAtUtc: DateTimeOffset.Parse("2026-06-02T00:00:00+00:00")),
                CreateNotebookSummary(
                    PublicNotebookId,
                    "Published Patterns",
                    "published-patterns",
                    canEdit: false,
                    lastActivityAtUtc: DateTimeOffset.Parse("2026-06-03T00:00:00+00:00"))
            ]
        };
        var tools = CreateTools(readService, maxToolResults: 10);

        var result = await tools.ListNotebooksAsync(cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(["published-patterns", "architecture-notes", "private-drafts"], result.Notebooks.Select(notebook => notebook.Slug));
        var architectureNotes = Assert.Single(result.Notebooks, notebook => notebook.Slug == "architecture-notes");
        Assert.True(architectureNotes.CanEdit);
        Assert.Equal(CurrentUserId, readService.MyNotebookUserIds.Single());
        Assert.Equal(CurrentUserId, readService.PublicNotebookUserIds.Single());
    }

    [Fact]
    public async Task ListNotebooksAsync_AppliesConfiguredToolLimitAfterCombiningAccessibleNotebooks()
    {
        var readService = new FakeNotebookReadService
        {
            MyNotebooks =
            [
                CreateNotebookSummary(
                    SharedNotebookId,
                    "Architecture Notes",
                    "architecture-notes",
                    canEdit: true,
                    lastActivityAtUtc: DateTimeOffset.Parse("2026-06-02T00:00:00+00:00"))
            ],
            PublicNotebooks =
            [
                CreateNotebookSummary(
                    PublicNotebookId,
                    "Published Patterns",
                    "published-patterns",
                    canEdit: false,
                    lastActivityAtUtc: DateTimeOffset.Parse("2026-06-03T00:00:00+00:00"))
            ]
        };
        var tools = CreateTools(readService, maxToolResults: 1);

        var result = await tools.ListNotebooksAsync(limit: 5, cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);
        var notebook = Assert.Single(result.Notebooks);
        Assert.Equal("published-patterns", notebook.Slug);
        Assert.Equal(1, readService.MyNotebookLimits.Single());
        Assert.Equal(1, readService.PublicNotebookLimits.Single());
    }

    [Fact]
    public async Task ListNotebooksAsync_ReturnsAuthenticationErrorWhenUserIsMissing()
    {
        var tools = CreateTools(new FakeNotebookReadService(), maxToolResults: 10, authenticated: false);

        var result = await tools.ListNotebooksAsync(cancellationToken: CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("authentication_required", result.Error?.Code);
        Assert.Empty(result.Notebooks);
    }

    [Fact]
    public async Task SearchNotesAsync_ReturnsVisibleNotebookItemsWithConfiguredLimit()
    {
        var readService = new FakeNotebookReadService
        {
            SearchResults =
            [
                CreateSearchResult(
                    "architecture-notes",
                    "Architecture Notes",
                    "guides/overview",
                    "Overview",
                    "Use adapter boundaries for AI integration.")
            ]
        };
        var tools = CreateTools(readService, maxToolResults: 3);

        var result = await tools.SearchNotesAsync("adapter boundaries", limit: 9, cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Results);
        Assert.Equal("architecture-notes", item.NotebookSlug);
        Assert.Equal("guides/overview", item.Path);
        Assert.Equal("Use adapter boundaries for AI integration.", item.PlainTextSnippet);
        Assert.Equal(CurrentUserId, readService.SearchUserIds.Single());
        Assert.Equal("adapter boundaries", readService.SearchQueries.Single());
        Assert.Equal(3, readService.SearchLimits.Single());
    }

    [Fact]
    public async Task SearchNotesAsync_ReturnsValidationErrorForBlankQuery()
    {
        var readService = new FakeNotebookReadService();
        var tools = CreateTools(readService, maxToolResults: 10);

        var result = await tools.SearchNotesAsync("   ", cancellationToken: CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_search", result.Error?.Code);
        Assert.Empty(readService.SearchQueries);
    }

    [Fact]
    public async Task GetNotebookAsync_ReturnsNotebookMetadataAndBoundedItemSummaries()
    {
        var readService = new FakeNotebookReadService
        {
            NotebookBySlugResult = NotesResult<NotebookDetailModel>.Success(CreateNotebookDetail(
                [
                    CreateNotebookItem("overview", "Overview", "A".PadRight(24, 'A')),
                    CreateNotebookItem("deep-dive", "Deep Dive", "B".PadRight(24, 'B'))
                ]))
        };
        var tools = CreateTools(readService, maxToolResults: 1, maxToolContentChars: 12);

        var result = await tools.GetNotebookAsync(
            "architecture-notes",
            includeItems: true,
            itemLimit: 5,
            cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("architecture-notes", result.Notebook?.Summary.Slug);
        var item = Assert.Single(result.Notebook!.Items);
        Assert.Equal("overview", item.Path);
        Assert.Equal("AAAAAAAAAAAA\n[truncated]", item.PlainTextContent);
        Assert.Equal(("architecture-notes", CurrentUserId, false, true), readService.NotebookBySlugCalls.Single());
    }

    [Fact]
    public async Task GetNotebookAsync_MapsReadServiceFailureToToolError()
    {
        var readService = new FakeNotebookReadService
        {
            NotebookBySlugResult = NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "You do not have access to this notebook.")
        };
        var tools = CreateTools(readService, maxToolResults: 10);

        var result = await tools.GetNotebookAsync("private-notes", cancellationToken: CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("notebook_forbidden", result.Error?.Code);
        Assert.Null(result.Notebook);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsOneVisibleNotebookItemByNormalizedPath()
    {
        var readService = new FakeNotebookReadService
        {
            NotebookBySlugResult = NotesResult<NotebookDetailModel>.Success(CreateNotebookDetail(
                [
                    CreateNotebookItem("guides/overview", "Overview", "Current page body")
                ]))
        };
        var tools = CreateTools(readService, maxToolResults: 10);

        var result = await tools.GetPageAsync(
            "architecture-notes",
            "/guides/overview/",
            cancellationToken: CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal("guides/overview", result.Item?.Path);
        Assert.Equal("Current page body", result.Item?.PlainTextContent);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsNotFoundWhenPathIsMissing()
    {
        var readService = new FakeNotebookReadService
        {
            NotebookBySlugResult = NotesResult<NotebookDetailModel>.Success(CreateNotebookDetail([]))
        };
        var tools = CreateTools(readService, maxToolResults: 10);

        var result = await tools.GetPageAsync(
            "architecture-notes",
            "guides/missing",
            cancellationToken: CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("notebook_item_not_found", result.Error?.Code);
        Assert.Null(result.Item);
    }

    private static NotebookAssistantTools CreateTools(
        FakeNotebookReadService readService,
        int maxToolResults,
        int maxToolContentChars = 4000,
        bool authenticated = true)
    {
        var httpContext = new DefaultHttpContext();
        if (authenticated)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, CurrentUserId.ToString())
            ], "test"));
        }

        return new NotebookAssistantTools(
            readService,
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(new AiOptions
            {
                MaxToolResults = maxToolResults,
                MaxToolContentChars = maxToolContentChars
            }));
    }

    private static NotebookSummaryModel CreateNotebookSummary(
        Guid id,
        string title,
        string slug,
        bool canEdit,
        DateTimeOffset lastActivityAtUtc)
        => new(
            id,
            CurrentUserId,
            title,
            slug,
            Description: null,
            Visibility: "public",
            IsPublished: true,
            AuthorDisplayName: "Yao",
            CanEdit: canEdit,
            ItemCount: 1,
            FolderCount: 0,
            PageCount: 1,
            FavoriteCount: 0,
            IsFavoritedByMe: false,
            LastActivityAtUtc: lastActivityAtUtc,
            CreatedAtUtc: lastActivityAtUtc,
            UpdatedAtUtc: lastActivityAtUtc,
            PublishedAtUtc: lastActivityAtUtc);

    private static NotebookDetailModel CreateNotebookDetail(IReadOnlyList<NotebookItemModel> items)
        => new(
            SharedNotebookId,
            CurrentUserId,
            "Architecture Notes",
            "architecture-notes",
            Description: "Architecture notebook",
            Visibility: "private",
            IsPublished: false,
            AuthorDisplayName: "Yao",
            CanEdit: true,
            ItemCount: items.Count,
            FolderCount: items.Count(item => item.Type == "folder"),
            PageCount: items.Count(item => item.Type == "page"),
            FavoriteCount: 0,
            IsFavoritedByMe: false,
            LastActivityAtUtc: DateTimeOffset.Parse("2026-06-02T00:00:00+00:00"),
            CreatedAtUtc: DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-06-02T00:00:00+00:00"),
            PublishedAtUtc: null,
            Items: items);

    private static NotebookItemModel CreateNotebookItem(
        string path,
        string title,
        string plainTextContent,
        string type = "page")
        => new(
            Guid.NewGuid(),
            SharedNotebookId,
            ParentId: null,
            Type: type,
            Title: title,
            Slug: path.Split('/').Last(),
            Path: path,
            SortOrder: 0,
            ContentFormat: "tiptap_json",
            ContentJson: null,
            PlainTextContent: plainTextContent,
            IsArchived: false,
            ArchivedAtUtc: null,
            ArchivedByUserId: null,
            CreatedAtUtc: DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-06-02T00:00:00+00:00"));

    private static NotebookItemSearchModel CreateSearchResult(
        string notebookSlug,
        string notebookTitle,
        string path,
        string title,
        string plainTextContent)
        => new(
            SharedNotebookId,
            notebookSlug,
            notebookTitle,
            NotebookCanEdit: true,
            ItemId: Guid.NewGuid(),
            path,
            title,
            Type: "page",
            plainTextContent,
            CreatedAtUtc: DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            UpdatedAtUtc: DateTimeOffset.Parse("2026-06-02T00:00:00+00:00"));

    private sealed class FakeNotebookReadService : INotebookReadService
    {
        public IReadOnlyList<NotebookSummaryModel> MyNotebooks { get; init; } = [];

        public IReadOnlyList<NotebookSummaryModel> PublicNotebooks { get; init; } = [];

        public IReadOnlyList<NotebookItemSearchModel> SearchResults { get; init; } = [];

        public NotesResult<NotebookDetailModel> NotebookBySlugResult { get; init; } =
            NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found.");

        public List<Guid> MyNotebookUserIds { get; } = [];

        public List<Guid> PublicNotebookUserIds { get; } = [];

        public List<Guid> SearchUserIds { get; } = [];

        public List<string> SearchQueries { get; } = [];

        public List<int?> MyNotebookLimits { get; } = [];

        public List<int?> PublicNotebookLimits { get; } = [];

        public List<int?> SearchLimits { get; } = [];

        public List<(string Slug, Guid UserId, bool IncludeArchived, bool IncludeItems)> NotebookBySlugCalls { get; } = [];

        public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
            string? search,
            Guid currentUserId,
            CancellationToken cancellationToken,
            int? limit = null)
        {
            PublicNotebookUserIds.Add(currentUserId);
            PublicNotebookLimits.Add(limit);
            return Task.FromResult(PublicNotebooks);
        }

        public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
            Guid currentUserId,
            string? search,
            CancellationToken cancellationToken,
            int? limit = null)
        {
            MyNotebookUserIds.Add(currentUserId);
            MyNotebookLimits.Add(limit);
            return Task.FromResult(MyNotebooks);
        }

        public Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
            Guid currentUserId,
            string search,
            CancellationToken cancellationToken,
            int? limit = null)
        {
            SearchUserIds.Add(currentUserId);
            SearchQueries.Add(search);
            SearchLimits.Add(limit);
            return Task.FromResult(SearchResults);
        }

        public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
            string slug,
            Guid currentUserId,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            bool includeItems = true)
            => Task.FromResult(NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."));

        public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(
            string slug,
            CancellationToken cancellationToken)
            => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."));

        public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(
            string slug,
            string path,
            CancellationToken cancellationToken)
            => Task.FromResult(NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."));

        public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
            Guid notebookId,
            Guid currentUserId,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            bool includeItems = true)
            => Task.FromResult(NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."));

        public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
            string slug,
            Guid currentUserId,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            bool includeItems = true)
        {
            NotebookBySlugCalls.Add((slug, currentUserId, includeArchived, includeItems));
            return Task.FromResult(NotebookBySlugResult);
        }

        public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
            Guid notebookId,
            Guid currentUserId,
            string? search,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            int? limit = null)
            => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."));
    }
}
