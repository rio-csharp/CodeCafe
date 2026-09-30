using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Domain.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class CreatePageCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesRootPage_AtNotebookRoot()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(
            new CreatePageCommand(notebook.Slug, "  Getting Started  ", null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var page = Assert.Single(pages);
        Assert.Equal("Getting Started", page.Title);
        Assert.Equal("getting-started", page.Slug);
        Assert.Null(page.ParentId);
        Assert.Equal("/getting-started", result.Value!.Path);
    }

    [Fact]
    public async Task Handle_CreatesChildPage_UnderResolvedParent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", SortKeys.First());
        var (handler, pages, _) = CreateHandler(owner.Id, notebook, parent);

        var result = await handler.Handle(
            new CreatePageCommand(notebook.Slug, "Child", "/parent"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var child = Assert.Single(pages, page => page.Id != parent.Id);
        Assert.Equal(parent.Id, child.ParentId);
        Assert.Equal("/parent/child", result.Value!.Path);
        Assert.Equal(child.Id, parent.FirstChildId);
    }

    [Fact]
    public async Task Handle_AppendsAfterLastSibling()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var first = Page.Create(notebook.Id, null, "First", "first", "a");
        var second = Page.Create(notebook.Id, null, "Second", "second", "b");
        first.SetNextSibling(second.Id);
        var (handler, pages, _) = CreateHandler(owner.Id, notebook, first, second);

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "Third", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var third = Assert.Single(pages, page => page.Title == "Third");
        Assert.Equal(third.Id, second.NextSiblingId);
        Assert.True(string.CompareOrdinal(second.SortKey, third.SortKey) < 0);
    }

    [Fact]
    public async Task Handle_RebalancesTheLevel_WhenTheTightKeyWouldGrowTooLong()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        // A key at the 64-char cap leaves no room to append, forcing a rebalance of the level.
        var first = Page.Create(notebook.Id, null, "First", "first", "a");
        var last = Page.Create(notebook.Id, null, "Last", "last", new string('z', 60));
        first.SetNextSibling(last.Id);
        var (handler, pages, _) = CreateHandler(owner.Id, notebook, first, last);

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "Third", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var third = Assert.Single(pages, page => page.Title == "Third");
        Assert.All(pages, page => Assert.InRange(page.SortKey.Length, 1, 2));
        Assert.True(string.CompareOrdinal(first.SortKey, last.SortKey) < 0);
        Assert.True(string.CompareOrdinal(last.SortKey, third.SortKey) < 0);
        Assert.Equal(third.Id, last.NextSiblingId);
    }

    [Fact]
    public async Task Handle_FirstRootPage_HeadsTheNotebookChain()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "First", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var page = Assert.Single(pages);
        Assert.Equal(page.Id, notebook.FirstPageId);
    }

    [Fact]
    public async Task Handle_AppendingAfterAParentWithChildren_LeavesItsChildChainAlone()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var child = Page.Create(notebook.Id, parent.Id, "Child", "child", "a");
        parent.SetFirstChild(child.Id);
        var (handler, pages, _) = CreateHandler(owner.Id, notebook, parent, child);

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "NewRoot", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var newRoot = Assert.Single(pages, page => page.Title == "NewRoot");
        Assert.Equal(newRoot.Id, parent.NextSiblingId); // appended through the sibling pointer
        Assert.Equal(child.Id, parent.FirstChildId); // the child chain is a separate pointer
    }

    [Fact]
    public async Task Handle_RetriesWithFreshSlug_WhenSaveRaces()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, unitOfWork) = CreateHandler(owner.Id, notebook);
        unitOfWork.FailuresRemaining = 1;

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "Race", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var page = Assert.Single(pages);
        Assert.NotEqual("race", page.Slug);
        Assert.StartsWith("race-", page.Slug);
    }

    [Fact]
    public async Task Handle_ReturnsParentNotFound_ForUnknownParentPath()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, _, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(
            new CreatePageCommand(notebook.Slug, "Child", "/missing"),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.ParentNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_DeniesViewerCollaborator()
    {
        var owner = SeedOwner();
        var viewer = User.Create("viewer@example.com", "viewer@example.com", "Viewer", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(viewer.Id, CollaboratorRole.Viewer);
        var (handler, pages, _) = CreateHandler(viewer.Id, notebook);

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "Nope", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Empty(pages);
    }

    [Fact]
    public async Task Handle_AllowsEditorCollaborator()
    {
        var owner = SeedOwner();
        var editor = User.Create("editor@example.com", "editor@example.com", "Editor", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(editor.Id, CollaboratorRole.Editor);
        var (handler, pages, _) = CreateHandler(editor.Id, notebook);

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "Draft", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(pages);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenNoCurrentUser()
    {
        var notebook = SeedNotebook(SeedOwner());
        var handler = new CreatePageCommandHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository { notebook },
            new StubPageRepository(),
            new StubUnitOfWork(new UniqueConstraintViolationException("IX_pages_NotebookId_Slug"))
        );

        var result = await handler.Handle(new CreatePageCommand(notebook.Slug, "Nope", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (CreatePageCommandHandler Handler, StubPageRepository Pages, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_pages_NotebookId_Slug"))
        {
            FailuresRemaining = 0,
        };
        return (
            new CreatePageCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                unitOfWork
            ),
            pages,
            unitOfWork
        );
    }
}
