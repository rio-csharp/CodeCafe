using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Trash.RestorePageFromTrash;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests;

public sealed class RestorePageFromTrashCommandHandlerTests
{
    [Fact]
    public async Task Handle_RestoresSubtree_AtEndOfOriginalParentChain()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var sibling = Page.Create(notebook.Id, parent.Id, "Sibling", "sibling", "a");
        parent.SetFirstChild(sibling.Id);
        var page = Trashed(notebook.Id, parent.Id, "Page", "page", "b");
        var child = Trashed(notebook.Id, page.Id, "Child", "child", "a");
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, parent, sibling, page, child);

        var result = await handler.Handle(new RestorePageFromTrashCommand(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Null(page.DeletedAtUtc);
        Assert.Null(child.DeletedAtUtc);
        Assert.Equal(parent.Id, page.ParentId);
        Assert.Equal(page.Id, sibling.NextSiblingId); // appended at the end of the child chain
        Assert.Null(page.NextSiblingId);
        Assert.Equal(sibling.Id, parent.FirstChildId); // the chain head is untouched
        Assert.True(string.CompareOrdinal(sibling.SortKey, page.SortKey) < 0);
        // Slugs were free, so both pages keep the ones they carried into the trash.
        Assert.Equal("page", page.Slug);
        Assert.Equal("child", child.Slug);
    }

    [Fact]
    public async Task Handle_TrashedParent_ReRootsAtNotebookChainEnd()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var root = Page.Create(notebook.Id, null, "Root", "root", "a");
        notebook.SetFirstPage(root.Id);
        var trashedParent = Trashed(notebook.Id, null, "Gone", "gone", "b");
        var page = Trashed(notebook.Id, trashedParent.Id, "Page", "page", "a");
        var (handler, _, _) = CreateHandler(owner.Id, notebook, root, trashedParent, page);

        var result = await handler.Handle(new RestorePageFromTrashCommand(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(page.DeletedAtUtc);
        Assert.Null(page.ParentId);
        Assert.Equal(page.Id, root.NextSiblingId); // appended at the end of the notebook root chain
        Assert.Equal(root.Id, notebook.FirstPageId);
        Assert.NotNull(trashedParent.DeletedAtUtc); // the trashed parent stays in the trash
    }

    [Fact]
    public async Task Handle_RekeysRoot_WhenLivePageHoldsItsSlug()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var squatter = Page.Create(notebook.Id, null, "Squatter", "page", "a");
        var page = Trashed(notebook.Id, null, "Page", "page", "b");
        var (handler, _, _) = CreateHandler(owner.Id, notebook, squatter, page);

        var result = await handler.Handle(new RestorePageFromTrashCommand(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Matches(@"^page-\d{4}$", page.Slug);
        Assert.Equal("page", squatter.Slug);
    }

    [Fact]
    public async Task Handle_RekeysDescendant_WhenLivePageHoldsItsSlug()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var squatter = Page.Create(notebook.Id, null, "Squatter", "child", "a");
        var page = Trashed(notebook.Id, null, "Page", "page", "b");
        var child = Trashed(notebook.Id, page.Id, "Child", "child", "a");
        var (handler, _, _) = CreateHandler(owner.Id, notebook, squatter, page, child);

        var result = await handler.Handle(new RestorePageFromTrashCommand(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("page", page.Slug); // the root's slug is free, so it stays
        Assert.Matches(@"^child-\d{4}$", child.Slug);
    }

    [Fact]
    public async Task Handle_RetryRekeysFromOriginalSlug_WhenSaveRaces()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        // The squatter keeps the base slug taken, so both attempts must re-key from "page"
        // rather than chaining a suffix onto the first attempt's candidate.
        var squatter = Page.Create(notebook.Id, null, "Squatter", "page", "a");
        var page = Trashed(notebook.Id, null, "Page", "page", "b");
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, squatter, page);
        unitOfWork.FailuresRemaining = 1;

        var result = await handler.Handle(new RestorePageFromTrashCommand(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Matches(@"^page-\d{4}$", page.Slug);
    }

    [Fact]
    public async Task Handle_UnknownPageId_IsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, _, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(new RestorePageFromTrashCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_LivePage_IsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var live = Page.Create(notebook.Id, null, "Live", "live", "a");
        var (handler, _, _) = CreateHandler(owner.Id, notebook, live);

        var result = await handler.Handle(new RestorePageFromTrashCommand(live.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Null(live.DeletedAtUtc);
    }

    [Fact]
    public async Task Handle_DeniesNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = Trashed(notebook.Id, null, "Page", "page", "a");
        var (handler, _, _) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(new RestorePageFromTrashCommand(page.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.NotNull(page.DeletedAtUtc);
    }

    [Fact]
    public async Task Handle_TrashedNotebook_IsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        var page = Trashed(notebook.Id, null, "Page", "page", "a");
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new RestorePageFromTrashCommand(page.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.NotNull(page.DeletedAtUtc);
    }

    private static Page Trashed(Guid notebookId, Guid? parentId, string title, string slug, string sortKey)
    {
        var page = Page.Create(notebookId, parentId, title, slug, sortKey);
        page.SoftDelete(DateTimeOffset.UtcNow);
        return page;
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (RestorePageFromTrashCommandHandler Handler, StubPageRepository Pages, StubUnitOfWork UnitOfWork) CreateHandler(
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
            new RestorePageFromTrashCommandHandler(
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
