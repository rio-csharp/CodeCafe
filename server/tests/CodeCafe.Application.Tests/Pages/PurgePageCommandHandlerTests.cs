using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Trash.PurgePage;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Pages;

public sealed class PurgePageCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesSubtree_AndFavoritesCascade()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Trashed(notebook.Id, null, "Page", "page", "a");
        var child = Trashed(notebook.Id, page.Id, "Child", "child", "a");
        var grandchild = Trashed(notebook.Id, child.Id, "Grandchild", "grandchild", "a");
        var unrelated = Trashed(notebook.Id, null, "Unrelated", "unrelated", "b");
        var live = Page.Create(notebook.Id, null, "Live", "live", "c");
        var (handler, pages, unitOfWork) = CreateHandler(owner.Id, notebook, page, child, grandchild, unrelated, live);
        pages.Favorites.Add((page.Id, owner.Id));
        pages.Favorites.Add((child.Id, owner.Id));
        pages.Favorites.Add((live.Id, owner.Id));

        var result = await handler.Handle(new PurgePageCommand(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(2, pages.Count);
        Assert.DoesNotContain(pages, candidate => candidate.Id == page.Id || candidate.Id == child.Id || candidate.Id == grandchild.Id);
        Assert.Contains(pages, candidate => candidate.Id == unrelated.Id);
        Assert.Equal([(live.Id, owner.Id)], pages.Favorites);
    }

    [Fact]
    public async Task Handle_LivePage_IsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var live = Page.Create(notebook.Id, null, "Live", "live", "a");
        var (handler, pages, _) = CreateHandler(owner.Id, notebook, live);

        var result = await handler.Handle(new PurgePageCommand(live.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Single(pages);
    }

    [Fact]
    public async Task Handle_DeniesNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = Trashed(notebook.Id, null, "Page", "page", "a");
        var (handler, pages, _) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(new PurgePageCommand(page.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Single(pages);
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

    private static (PurgePageCommandHandler Handler, StubPageRepository Pages, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var unitOfWork = new StubUnitOfWork();
        return (
            new PurgePageCommandHandler(
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
