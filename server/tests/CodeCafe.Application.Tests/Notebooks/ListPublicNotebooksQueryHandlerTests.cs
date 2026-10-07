using CodeCafe.Application.Notebooks.ListPublicNotebooks;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class ListPublicNotebooksQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPublicNotebooksOnly()
    {
        var owner = SeedUser("owner@example.com");
        var publicNotebook = SeedNotebook(owner, "public-notebook", NotebookVisibility.Public);
        var unlisted = SeedNotebook(owner, "unlisted-notebook", NotebookVisibility.Unlisted);
        var privateNotebook = SeedNotebook(owner, "private-notebook");
        var handler = CreateHandler(publicNotebook, unlisted, privateNotebook);

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("public-notebook", item.Slug);
        Assert.False(item.IsFavorite);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_SearchesThePublicCatalog()
    {
        var owner = SeedUser("owner@example.com");
        var match = SeedNotebook(owner, "rust-notes", NotebookVisibility.Public);
        var noMatch = SeedNotebook(owner, "gardening", NotebookVisibility.Public);
        var handler = CreateHandler(match, noMatch);

        var result = await handler.Handle(Query() with { Search = "rust" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("rust-notes", item.Slug);
    }

    [Fact]
    public async Task Handle_PaginatesWithOffset()
    {
        var owner = SeedUser("owner@example.com");
        var notebooks = Enumerable
            .Range(0, 3)
            .Select(index => SeedNotebook(owner, $"notebook-{index}", NotebookVisibility.Public))
            .ToList();
        var handler = CreateHandler(notebooks.ToArray());

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
    }

    private static ListPublicNotebooksQuery Query() => new(null, null, null, null);

    [Fact]
    public async Task Handle_ReturnsFavoriteStateForSignedInUser()
    {
        var owner = SeedUser("owner@example.com");
        var viewer = SeedUser("viewer@example.com");
        var notebook = SeedNotebook(owner, "brew-guide", NotebookVisibility.Public);
        var repository = new StubNotebookRepository { notebook };
        repository.Favorites.Add((notebook.Id, viewer.Id));
        var handler = new ListPublicNotebooksQueryHandler(
            repository,
            new StubPageRepository(),
            new StubUserRepository { owner },
            new StubCurrentUserAccessor(new CurrentUser(viewer.Id))
        );

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(Assert.Single(result.Value!.Items).IsFavorite);
    }

    [Fact]
    public async Task Handle_AttributesEachNotebookToItsOwnersDisplayName()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "brew-guide", NotebookVisibility.Public);
        var repository = new StubNotebookRepository { notebook };
        var users = new StubUserRepository { owner };
        var handler = new ListPublicNotebooksQueryHandler(
            repository,
            new StubPageRepository(),
            users,
            new StubCurrentUserAccessor(null)
        );

        var result = await handler.Handle(Query(), CancellationToken.None);

        Assert.Equal("User", Assert.Single(result.Value!.Items).OwnerDisplayName);
    }

    private static User SeedUser(string email) => User.Create(email, email, "User", "hash");

    private static Notebook SeedNotebook(
        User owner,
        string slug,
        NotebookVisibility visibility = NotebookVisibility.Private
    )
        => Notebook.Create(owner.Id, $"Title {slug}", null, slug, visibility);

    private static ListPublicNotebooksQueryHandler CreateHandler(params Notebook[] notebooks)
    {
        var repository = new StubNotebookRepository();
        repository.AddRange(notebooks);
        return new ListPublicNotebooksQueryHandler(
            repository,
            new StubPageRepository(),
            new StubUserRepository(),
            new StubCurrentUserAccessor(null)
        );
    }
}
