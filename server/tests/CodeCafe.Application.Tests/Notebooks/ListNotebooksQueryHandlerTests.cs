using CodeCafe.Application.Auth;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class ListNotebooksQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOwnAndShared_ExcludingOthers()
    {
        var owner = SeedUser("owner@example.com");
        var other = SeedUser("other@example.com");
        var own = SeedNotebook(owner, "own-notebook");
        var sharedWithOwner = SeedNotebook(other, "shared-notebook");
        sharedWithOwner.Share(owner.Id, CollaboratorRole.Viewer);
        var invisible = SeedNotebook(other, "invisible-notebook");
        var handler = CreateHandler(owner, [own, sharedWithOwner, invisible]);

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.DoesNotContain(result.Value.Items, item => item.Slug == "invisible-notebook");
    }

    [Fact]
    public async Task Handle_FiltersByFavorite()
    {
        var owner = SeedUser("owner@example.com");
        var favorite = SeedNotebook(owner, "favorite-notebook");
        var plain = SeedNotebook(owner, "plain-notebook");
        var repository = new StubNotebookRepository { favorite, plain };
        repository.Favorites.Add((favorite.Id, owner.Id));
        var handler = new ListNotebooksQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            repository,
            new StubPageRepository(),
            new StubUserRepository()
        );

        var result = await handler.Handle(Query() with { IsFavorite = true }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("favorite-notebook", item.Slug);
        Assert.True(item.IsFavorite);
    }

    [Fact]
    public async Task Handle_FavoriteFilterOnlySeesTheCurrentUsersFavorites()
    {
        var owner = SeedUser("owner@example.com");
        var other = SeedUser("other@example.com");
        var notebook = SeedNotebook(owner, "shared-notebook");
        notebook.Share(other.Id, CollaboratorRole.Viewer);
        var repository = new StubNotebookRepository { notebook };
        // The other user favorited it; the owner's favorite filter must not see that.
        repository.Favorites.Add((notebook.Id, other.Id));
        var handler = new ListNotebooksQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            repository,
            new StubPageRepository(),
            new StubUserRepository()
        );

        var result = await handler.Handle(Query() with { IsFavorite = true }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_PaginatesWithOffset()
    {
        var owner = SeedUser("owner@example.com");
        var notebooks = new List<Notebook>();
        for (var index = 0; index < 3; index++)
        {
            notebooks.Add(SeedNotebook(owner, $"notebook-{index}"));
        }

        var handler = CreateHandler(owner, notebooks);

        var firstPage = await handler.Handle(Query() with { PageSize = 2 }, CancellationToken.None);
        var secondPage = await handler.Handle(
            Query() with { PageSize = 2, Page = 2 },
            CancellationToken.None
        );

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(2, firstPage.Value!.Items.Count);
        Assert.Equal(3, firstPage.Value.TotalCount);
        Assert.True(firstPage.Value.HasNextPage);

        Assert.True(secondPage.IsSuccess);
        Assert.Single(secondPage.Value!.Items);
        Assert.False(secondPage.Value.HasNextPage);
        Assert.Empty(secondPage.Value.Items.Select(i => i.Id).Intersect(firstPage.Value.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task Handle_SortsByTitleAscending()
    {
        var owner = SeedUser("owner@example.com");
        var handler = CreateHandler(
            owner,
            [SeedNotebook(owner, "zebra"), SeedNotebook(owner, "apple"), SeedNotebook(owner, "mango")]
        );

        var result = await handler.Handle(Query() with { Sort = NotebookSort.TitleAsc }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Title apple", "Title mango", "Title zebra"], result.Value!.Items.Select(item => item.Title));
    }

    [Fact]
    public async Task Handle_SortsByUpdatedAtDescending_ByDefault()
    {
        var owner = SeedUser("owner@example.com");
        var older = SeedNotebook(owner, "older");
        var newer = SeedNotebook(owner, "newer");
        older.UpdateDetails("Title older", null, NotebookVisibility.Private); // Touch() is timestamped
        var handler = CreateHandler(owner, [newer, older]);

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("older", result.Value!.Items[0].Slug);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenNoCurrentUser()
    {
        var handler = new ListNotebooksQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository(),
            new StubPageRepository(),
            new StubUserRepository()
        );

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_SearchesTitleAndDescription_CaseInsensitively()
    {
        var owner = SeedUser("owner@example.com");
        var titleMatch = SeedNotebook(owner, "rust-notes");
        var descriptionMatch = Notebook.Create(
            owner.Id,
            "Untitled",
            "A tour of Rust ownership",
            "ownership",
            NotebookVisibility.Private
        );
        var noMatch = SeedNotebook(owner, "gardening");
        var handler = CreateHandler(owner, [titleMatch, descriptionMatch, noMatch]);

        var result = await handler.Handle(Query() with { Search = "RUST" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // The count and the page must agree, which is what NotebookFilter exists to guarantee.
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(
            ["ownership", "rust-notes"],
            result.Value.Items.Select(item => item.Slug).OrderBy(slug => slug, StringComparer.Ordinal)
        );
    }

    [Fact]
    public async Task Handle_SearchesNothing_WhenTheTermIsBlank()
    {
        var owner = SeedUser("owner@example.com");
        var handler = CreateHandler(owner, [SeedNotebook(owner, "rust-notes"), SeedNotebook(owner, "gardening")]);

        var result = await handler.Handle(Query() with { Search = "   " }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
    }

    [Fact]
    public async Task Handle_ReturnsRealPageCounts_ExcludingTrashedPages()
    {
        var owner = SeedUser("owner@example.com");
        var withPages = SeedNotebook(owner, "with-pages");
        var empty = SeedNotebook(owner, "empty");
        var trashed = Page.Create(withPages.Id, null, "Gone", "gone", "a2");
        trashed.SoftDelete(DateTimeOffset.UtcNow);
        var pages = new StubPageRepository
        {
            Page.Create(withPages.Id, null, "One", "one", "a0"),
            Page.Create(withPages.Id, null, "Two", "two", "a1"),
            trashed,
        };
        var handler = CreateHandler(owner, [withPages, empty], pages);

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Single(item => item.Slug == "with-pages").PageCount);
        Assert.Equal(0, result.Value.Items.Single(item => item.Slug == "empty").PageCount);
    }

    private static ListNotebooksQuery Query() => new(null, null, null, null, null, null, null);

    private static User SeedUser(string email) => User.Create(email, email, "User", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, $"Title {slug}", null, slug, NotebookVisibility.Private);

    private static ListNotebooksQueryHandler CreateHandler(
        User currentUser,
        IEnumerable<Notebook> notebooks,
        StubPageRepository? pages = null
    )
    {
        var repository = new StubNotebookRepository();
        repository.AddRange(notebooks);
        return new ListNotebooksQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            repository,
            pages ?? new StubPageRepository(),
            new StubUserRepository()
        );
    }
}
