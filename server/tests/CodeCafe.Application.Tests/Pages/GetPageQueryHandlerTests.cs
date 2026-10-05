using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.GetPage;
using CodeCafe.Application.Pages.GetPageByPath;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

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

    [Fact]
    public async Task Handle_HidesShares_FromPageShareGuest()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner); // private
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(guest.Id, CollaboratorRole.Viewer);

        var result = await CreateHandler(guest.Id, notebook, [page], guest)
            .Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Shares);
    }

    [Fact]
    public async Task Handle_ReturnsShares_WithDisplayNames_ToOwner()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(guest.Id, CollaboratorRole.Viewer);

        var result = await CreateHandler(owner.Id, notebook, [page], guest)
            .Handle(new GetPageQuery(page.Id), CancellationToken.None);

        var share = Assert.Single(result.Value!.Shares);
        Assert.Equal(guest.Id, share.UserId);
        Assert.Equal("Guest", share.UserName);
    }

    [Fact]
    public async Task Handle_ReturnsBlocksInDfsPreOrder()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "Page", "page", "a");
        var a = NewBlock(page, "a");
        var b = NewBlock(page, "b");
        var a1 = NewBlock(page, "a.a", a);
        var a2 = NewBlock(page, "a.b", a);
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(a1, page, a, after: null);
        BlockChain.Link(a2, page, a, after: a1);

        var handler = new GetPageQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository { b, a2, a, a1 }, // seeding order must not matter
            new StubUserRepository(),
            new StubPasswordHasher()
        );

        var result = await handler.Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([a.Id, a1.Id, a2.Id, b.Id], result.Value!.Blocks.Select(block => block.Id).ToArray());
        Assert.All(result.Value.Blocks, block => Assert.Equal("paragraph", block.Type));
    }

    [Fact]
    public async Task Handle_Owner_CanWrite()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");

        var result = await CreateHandler(owner.Id, notebook, page)
            .Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanWrite);
    }

    [Fact]
    public async Task Handle_NotebookEditor_CanWrite()
    {
        var owner = SeedOwner();
        var editor = User.Create("editor@example.com", "editor@example.com", "Editor", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(editor.Id, CollaboratorRole.Editor);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");

        var result = await CreateHandler(editor.Id, notebook, page)
            .Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanWrite);
    }

    [Fact]
    public async Task Handle_EditorPageShare_CanWrite()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner); // private
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(guest.Id, CollaboratorRole.Editor);

        var result = await CreateHandler(guest.Id, notebook, page)
            .Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanWrite);
    }

    [Fact]
    public async Task Handle_ViewerPageShare_CannotWrite()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner); // private
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(guest.Id, CollaboratorRole.Viewer);

        var result = await CreateHandler(guest.Id, notebook, page)
            .Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanWrite);
    }

    [Fact]
    public async Task Handle_AnonymousReader_CannotWrite()
    {
        var owner = SeedOwner();
        var notebook = Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Public);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var handler = new GetPageQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository(),
            new StubUserRepository(),
            new StubPasswordHasher()
        );

        var result = await handler.Handle(new GetPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanWrite);
    }

    private static Block NewBlock(Page page, string sortKey, Block? parent = null)
        => Block.Create(page.Id, parent?.Id, "paragraph", """{"spans":[]}""", string.Empty, sortKey);

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static GetPageQueryHandler CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page[] seededPages,
        params User[] users
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var usersStub = new StubUserRepository();
        usersStub.AddRange(users);
        return new GetPageQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
            new StubNotebookRepository { notebook },
            pages,
            new StubBlockRepository(),
            usersStub,
            new StubPasswordHasher()
        );
    }

    private static GetPageQueryHandler CreateHandler(Guid currentUserId, Notebook notebook, params Page[] seededPages)
        => CreateHandler(currentUserId, notebook, seededPages, []);
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
            new StubBlockRepository(),
            new StubUserRepository(),
            new StubPasswordHasher()
        );
    }
}
