using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.GetNotebookTree;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class GetNotebookTreeQueryHandlerTests
{
    [Fact]
    public async Task Handle_OwnerSeesFullTree_InSortKeyOrder()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var b = Page.Create(notebook.Id, null, "B", "b", "b");
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var child = Page.Create(notebook.Id, a.Id, "Child", "child", "a");

        var result = await CreateHandler(owner.Id, notebook, b, a, child)
            .Handle(new GetNotebookTreeQuery(notebook.Slug), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var roots = result.Value!.Roots;
        Assert.Equal(["A", "B"], roots.Select(node => node.Title).ToArray());
        Assert.Equal([0, 1], roots.Select(node => node.SortOrder).ToArray());
        var childNode = Assert.Single(roots[0].Children);
        Assert.Equal("/a/child", childNode.Path);
    }

    [Fact]
    public async Task Handle_PageShareUserSeesOnlyTheSharedSubtree()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner); // private
        var shared = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        var child = Page.Create(notebook.Id, shared.Id, "Child", "child", "a");
        var unrelated = Page.Create(notebook.Id, null, "Unrelated", "unrelated", "b");
        shared.Share(guest.Id, CollaboratorRole.Viewer);

        var result = await CreateHandler(guest.Id, notebook, shared, child, unrelated)
            .Handle(new GetNotebookTreeQuery(notebook.Slug), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var root = Assert.Single(result.Value!.Roots);
        Assert.Equal("Shared", root.Title);
        Assert.Equal("Child", Assert.Single(root.Children).Title);
    }

    [Fact]
    public async Task Handle_MarksTheCallersFavorites()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var starred = Page.Create(notebook.Id, null, "Starred", "starred", "a");
        var plain = Page.Create(notebook.Id, null, "Plain", "plain", "b");
        var pages = new StubPageRepository { starred, plain };
        await pages.SetFavoriteAsync(starred.Id, owner.Id, true, CancellationToken.None);
        var handler = new GetNotebookTreeQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            pages,
            new StubPasswordHasher()
        );

        var result = await handler.Handle(new GetNotebookTreeQuery(notebook.Slug), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var roots = result.Value!.Roots;
        Assert.True(roots.Single(node => node.Title == "Starred").IsFavorite);
        Assert.False(roots.Single(node => node.Title == "Plain").IsFavorite);
    }

    [Fact]
    public async Task Handle_StrangerGetsNotFound_ForPrivateNotebook()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);

        var result = await CreateHandler(stranger.Id, notebook)
            .Handle(new GetNotebookTreeQuery(notebook.Slug), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static GetNotebookTreeQueryHandler CreateHandler(Guid currentUserId, Notebook notebook, params Page[] seededPages)
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        return new GetNotebookTreeQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
            new StubNotebookRepository { notebook },
            pages,
            new StubPasswordHasher()
        );
    }
}
