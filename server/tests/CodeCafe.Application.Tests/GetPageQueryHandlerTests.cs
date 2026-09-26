using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.GetPage;
using CodeCafe.Application.Pages.GetPageByPath;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests;

public sealed class GetPageQueryHandlerTests
{
    [Fact]
    public async Task Handle_OwnerReadsPage_WithFullPath()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var child = Page.Create(notebook.Id, parent.Id, "Child", "child", "a");

        var result = await CreateHandler(owner.Id, notebook, parent, child)
            .Handle(new GetPageQuery(child.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("/parent/child", result.Value!.Path);
    }

    [Fact]
    public async Task Handle_PageShareGrantsRead_WithoutNotebookAccess()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner); // private
        var shared = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        var child = Page.Create(notebook.Id, shared.Id, "Child", "child", "a");
        shared.Share(guest.Id, CollaboratorRole.Viewer);

        // The share is on the parent, so the child is reachable too.
        var result = await CreateHandler(guest.Id, notebook, shared, child)
            .Handle(new GetPageQuery(child.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("/shared/child", result.Value!.Path);
    }

    [Fact]
    public async Task Handle_StrangerGetsNotFound()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");

        var result = await CreateHandler(stranger.Id, notebook, page)
            .Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static GetPageQueryHandler CreateHandler(Guid currentUserId, Notebook notebook, params Page[] seededPages)
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        return new GetPageQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
            new StubNotebookRepository { notebook },
            pages,
            new StubUserRepository(),
            new StubPasswordHasher()
        );
    }
}

public sealed class GetPageByPathQueryHandlerTests
{
    [Fact]
    public async Task Handle_ResolvesNestedPath()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var child = Page.Create(notebook.Id, parent.Id, "Child", "child", "a");

        var result = await CreateHandler(owner.Id, notebook, parent, child)
            .Handle(new GetPageByPathQuery(notebook.Slug, "/parent/child"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(child.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_RejectsPathWhosePrefixDoesNotMatchTheAncestors()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var child = Page.Create(notebook.Id, parent.Id, "Child", "child", "a");

        // "child" exists, but not under "/wrong".
        var result = await CreateHandler(owner.Id, notebook, parent, child)
            .Handle(new GetPageByPathQuery(notebook.Slug, "/wrong/child"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownNotebookGivesNotebookNotFound()
    {
        var owner = SeedOwner();

        var result = await CreateHandler(owner.Id, SeedNotebook(owner))
            .Handle(new GetPageByPathQuery("no-such-notebook", "/a"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static GetPageByPathQueryHandler CreateHandler(Guid currentUserId, Notebook notebook, params Page[] seededPages)
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        return new GetPageByPathQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
            new StubNotebookRepository { notebook },
            pages,
            new StubUserRepository(),
            new StubPasswordHasher()
        );
    }
}
