using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Queries.GetNotebookItems;

namespace CodeCafe.Application.Tests;

public sealed class GetNotebookItemsQueryHandlerTests
{
    [Fact]
    public async Task IncludeArchived_Rejects_NonOwner()
    {
        var notebookId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var service = new StubNotebookQueryService
        {
            NotebookByIdResult = NotesResult<NotebookDetailModel>.Success(
                new NotebookDetailModel(
                    notebookId,
                    Guid.NewGuid(),
                    "Title",
                    "title",
                    null,
                    "public",
                    true,
                    "Author",
                    false,
                    0,
                    0,
                    0,
                    0,
                    false,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null,
                    null,
                    []
                )
            ),
        };

        var handler = new GetNotebookItemsQueryHandler(service);
        var result = await handler.Handle(
            new GetNotebookItemsQuery(notebookId, currentUserId, null, true),
            CancellationToken.None
        );

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Forbidden, result.Error?.Kind);
        Assert.False(service.GetNotebookItemsCalled);
    }

    [Fact]
    public async Task ExcludingArchived_Delegates_Directly_To_QueryService()
    {
        var notebookId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var expected = NotesResult<IReadOnlyList<NotebookItemModel>>.Success([]);
        var service = new StubNotebookQueryService { NotebookItemsResult = expected };

        var handler = new GetNotebookItemsQueryHandler(service);
        var result = await handler.Handle(
            new GetNotebookItemsQuery(notebookId, currentUserId, "hello", false),
            CancellationToken.None
        );

        Assert.True(result.Succeeded);
        Assert.True(service.GetNotebookItemsCalled);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task IncludeContent_Is_Passed_To_QueryService(bool includeContent, bool expected)
    {
        var notebookId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var service = new StubNotebookQueryService
        {
            NotebookItemsResult = NotesResult<IReadOnlyList<NotebookItemModel>>.Success([]),
        };

        var handler = new GetNotebookItemsQueryHandler(service);
        var result = await handler.Handle(
            new GetNotebookItemsQuery(notebookId, currentUserId, null, false, includeContent),
            CancellationToken.None
        );

        Assert.True(result.Succeeded);
        Assert.True(service.GetNotebookItemsCalled);
        Assert.Equal(expected, service.LastIncludeContent);
    }

    private sealed class StubNotebookQueryService : INotebookReadService
    {
        public NotesResult<NotebookDetailModel> NotebookByIdResult { get; init; } =
            NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.NotFound,
                "missing",
                "missing"
            );

        public NotesResult<IReadOnlyList<NotebookItemModel>> NotebookItemsResult { get; init; } =
            NotesResult<IReadOnlyList<NotebookItemModel>>.Success([]);

        public bool GetNotebookItemsCalled { get; private set; }

        public bool? LastIncludeContent { get; private set; }

        public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
            string? search,
            Guid currentUserId,
            CancellationToken cancellationToken,
            int? limit = null,
            int? offset = null
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
            Guid currentUserId,
            string? search,
            CancellationToken cancellationToken,
            int? limit = null,
            int? offset = null
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
            Guid currentUserId,
            string search,
            CancellationToken cancellationToken,
            int? limit = null
        ) => throw new NotSupportedException();

        public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
            string slug,
            Guid currentUserId,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            bool includeItems = true,
            bool includeContent = true
        ) => throw new NotSupportedException();

        public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(
            string slug,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(
            string slug,
            string path,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
            Guid notebookId,
            Guid currentUserId,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            bool includeItems = true,
            bool includeContent = true
        ) => Task.FromResult(NotebookByIdResult);

        public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
            string slug,
            Guid currentUserId,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            bool includeItems = true,
            bool includeContent = true
        ) => throw new NotSupportedException();

        public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
            Guid notebookId,
            Guid currentUserId,
            string? search,
            CancellationToken cancellationToken,
            bool includeArchived = false,
            bool includeContent = true,
            int? limit = null
        )
        {
            GetNotebookItemsCalled = true;
            LastIncludeContent = includeContent;
            return Task.FromResult(NotebookItemsResult);
        }

        public Task<NotesResult<NotebookItemModel>> GetNotebookItemByIdAsync(
            Guid notebookId,
            Guid itemId,
            Guid currentUserId,
            CancellationToken cancellationToken,
            bool includeArchived = false
        ) => throw new NotSupportedException();
    }
}
