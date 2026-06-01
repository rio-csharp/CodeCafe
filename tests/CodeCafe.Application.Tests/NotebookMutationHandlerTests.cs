using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.AddNotebookFavorite;
using CodeCafe.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Application.Notes.Commands.DeleteNotebook;
using CodeCafe.Application.Notes.Commands.RemoveNotebookFavorite;
using CodeCafe.Application.Notes.Commands.UpdateNotebook;
using CodeCafe.Application.Notes.Queries.GetNotebookFavoriteStatus;
using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Tests;

public sealed class NotebookMutationHandlerTests
{
    [Fact]
    public async Task CreateNotebookHandler_Creates_Notebook_And_Queries_Detail()
    {
        var store = new StubNotebookMutationStore();
        var queryService = new StubNotebookQueryService();
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var handler = new CreateNotebookCommandHandler(store, queryService, new StubDateTimeProvider(now));

        var result = await handler.Handle(
            new CreateNotebookCommand(Guid.NewGuid(), "  New Notebook  ", "  Description  ", "public"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(store.AddedNotebook);
        Assert.Equal("New Notebook", store.AddedNotebook!.Title);
        Assert.Equal("Description", store.AddedNotebook.Description);
        Assert.Equal(now, store.AddedNotebook.PublishedAtUtc);
        Assert.Equal(store.AddedNotebook.Id, queryService.LastNotebookId);
    }

    [Fact]
    public async Task UpdateNotebookHandler_Returns_Forbidden_When_NotOwner()
    {
        var store = new StubNotebookMutationStore
        {
            ExistingNotebook = null,
            NotebookExists = true
        };
        var handler = new UpdateNotebookCommandHandler(store, new StubNotebookQueryService(), new StubDateTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.Handle(
            new UpdateNotebookCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated", null, "private"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Forbidden, result.Error?.Kind);
    }

    [Fact]
    public async Task DeleteNotebookHandler_Removes_Owned_Notebook()
    {
        var notebook = CreateNotebook(Guid.NewGuid());
        var store = new StubNotebookMutationStore
        {
            ExistingNotebook = notebook,
            NotebookExists = true
        };
        var handler = new DeleteNotebookCommandHandler(store);

        var result = await handler.Handle(
            new DeleteNotebookCommand(notebook.Id, notebook.OwnerId),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Same(notebook, store.RemovedNotebook);
        Assert.True(store.SaveChangesCalled);
    }

    [Fact]
    public async Task GetFavoriteStatusHandler_Rejects_Inaccessible_Notebook()
    {
        var store = new StubNotebookMutationStore
        {
            LookupNotebook = CreateNotebook(Guid.NewGuid(), NotebookVisibility.Private, isPublished: false)
        };
        var handler = new GetNotebookFavoriteStatusQueryHandler(store);

        var result = await handler.Handle(
            new GetNotebookFavoriteStatusQuery(store.LookupNotebook.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Forbidden, result.Error?.Kind);
    }

    [Fact]
    public async Task AddFavoriteHandler_Adds_New_Favorite()
    {
        var notebook = CreateNotebook(Guid.NewGuid(), NotebookVisibility.Public, isPublished: true);
        var currentUserId = Guid.NewGuid();
        var store = new StubNotebookMutationStore
        {
            LookupNotebook = notebook,
            FavoriteCount = 1,
            FavoritedByCurrentUser = false
        };
        var handler = new AddNotebookFavoriteCommandHandler(store);

        var result = await handler.Handle(
            new AddNotebookFavoriteCommand(notebook.Id, currentUserId),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(store.AddedFavorite);
        Assert.True(store.SaveChangesCalled);
    }

    [Fact]
    public async Task RemoveFavoriteHandler_Removes_Existing_Favorite()
    {
        var notebook = CreateNotebook(Guid.NewGuid(), NotebookVisibility.Public, isPublished: true);
        var currentUserId = Guid.NewGuid();
        var existingFavorite = new NotebookFavorite
        {
            Id = Guid.NewGuid(),
            NotebookId = notebook.Id,
            UserId = currentUserId
        };
        var store = new StubNotebookMutationStore
        {
            LookupNotebook = notebook,
            ExistingFavorite = existingFavorite,
            FavoriteCount = 0,
            FavoritedByCurrentUser = false
        };
        var handler = new RemoveNotebookFavoriteCommandHandler(store);

        var result = await handler.Handle(
            new RemoveNotebookFavoriteCommand(notebook.Id, currentUserId),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Same(existingFavorite, store.RemovedFavorite);
    }

    private static Notebook CreateNotebook(Guid ownerId, NotebookVisibility visibility = NotebookVisibility.Private, bool isPublished = false)
    {
        return new Notebook
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = "Notebook",
            Slug = "notebook",
            Visibility = visibility,
            IsPublished = isPublished,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private sealed class StubDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class StubNotebookQueryService : INotebookQueryService, INotebookReadService
    {
        public Guid LastNotebookId { get; private set; }

        public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(string? search, Guid currentUserId, CancellationToken cancellationToken, int? limit = null)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(Guid currentUserId, string? search, CancellationToken cancellationToken, int? limit = null)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(Guid currentUserId, string search, CancellationToken cancellationToken, int? limit = null)
            => throw new NotSupportedException();

        public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
            => throw new NotSupportedException();

        public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        {
            LastNotebookId = notebookId;
            return Task.FromResult(NotesResult<NotebookDetailModel>.Success(
                new NotebookDetailModel(
                    notebookId,
                    currentUserId,
                    "Notebook",
                    "notebook",
                    null,
                    "private",
                    false,
                    "Yao",
                    true,
                    0,
                    0,
                    0,
                    0,
                    false,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null,
                    null,
                    [])));
        }

        public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
            => throw new NotSupportedException();

        public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(Guid notebookId, Guid currentUserId, string? search, CancellationToken cancellationToken, bool includeArchived = false, int? limit = null)
            => throw new NotSupportedException();
    }

    private sealed class StubNotebookMutationStore : INotebookMutationStore
    {
        public Notebook? AddedNotebook { get; private set; }
        public Notebook? RemovedNotebook { get; private set; }
        public NotebookFavorite? AddedFavorite { get; private set; }
        public NotebookFavorite? RemovedFavorite { get; private set; }
        public bool SaveChangesCalled { get; private set; }
        public Notebook? ExistingNotebook { get; init; }
        public Notebook? LookupNotebook { get; init; }
        public bool NotebookExists { get; init; }
        public NotebookFavorite? ExistingFavorite { get; init; }
        public int FavoriteCount { get; init; }
        public bool FavoritedByCurrentUser { get; init; }

        public Task<Notebook?> GetOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
            => Task.FromResult(ExistingNotebook);

        public Task<Notebook?> GetNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
            => Task.FromResult(LookupNotebook);

        public Task<bool> NotebookExistsAsync(Guid notebookId, CancellationToken cancellationToken)
            => Task.FromResult(NotebookExists);

        public Task<string> GenerateUniqueNotebookSlugAsync(string title, Guid? currentNotebookId, CancellationToken cancellationToken)
            => Task.FromResult("new-notebook");

        public void AddNotebook(Notebook notebook)
        {
            AddedNotebook = notebook;
        }

        public Task SaveNotebookAsync(Notebook notebook, string title, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void RemoveNotebook(Notebook notebook)
        {
            RemovedNotebook = notebook;
        }

        public Task<NotebookFavorite?> GetFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
            => Task.FromResult(ExistingFavorite);

        public void AddFavorite(NotebookFavorite favorite)
        {
            AddedFavorite = favorite;
        }

        public void RemoveFavorite(NotebookFavorite favorite)
        {
            RemovedFavorite = favorite;
        }

        public Task<int> CountFavoritesAsync(Guid notebookId, CancellationToken cancellationToken)
            => Task.FromResult(FavoriteCount);

        public Task<bool> IsFavoritedAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
            => Task.FromResult(FavoritedByCurrentUser);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
