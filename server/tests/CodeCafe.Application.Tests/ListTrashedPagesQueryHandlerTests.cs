using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Trash.ListTrashedPages;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests;

public sealed class ListTrashedPagesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ListsTrashedRootsOnly_WithDescendantCounts()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var deletedAtUtc = DateTimeOffset.UtcNow;
        var root = Trashed(notebook.Id, null, "Root", "root", "a", deletedAtUtc);
        var child = Trashed(notebook.Id, root.Id, "Child", "child", "a", deletedAtUtc);
        var grandchild = Trashed(notebook.Id, child.Id, "Grandchild", "grandchild", "a", deletedAtUtc);
        var standalone = Trashed(notebook.Id, null, "Standalone", "standalone", "b", deletedAtUtc.AddMinutes(1));
        var live = Page.Create(notebook.Id, null, "Live", "live", "c");
        var (handler, _) = CreateHandler(owner.Id, notebook, root, child, grandchild, standalone, live);

        var result = await handler.Handle(new ListTrashedPagesQuery(notebook.Slug, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        // Newest first.
        Assert.Equal(standalone.Id, result.Value.Items[0].PageId);
        var entry = result.Value.Items[1];
        Assert.Equal(root.Id, entry.PageId);
        Assert.Equal("root", entry.Slug);
        Assert.Equal(2, entry.DescendantCount);
        Assert.Equal(deletedAtUtc, entry.DeletedAtUtc);
        Assert.DoesNotContain(result.Value.Items, item => item.PageId == child.Id || item.PageId == live.Id);
    }

    [Fact]
    public async Task Handle_PageWithLiveParent_IsItsOwnRoot()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var liveParent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var orphan = Trashed(notebook.Id, liveParent.Id, "Orphan", "orphan", "a", DateTimeOffset.UtcNow);
        var (handler, _) = CreateHandler(owner.Id, notebook, liveParent, orphan);

        var result = await handler.Handle(new ListTrashedPagesQuery(notebook.Slug, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(result.Value!.Items);
        Assert.Equal(orphan.Id, entry.PageId);
        Assert.Equal(0, entry.DescendantCount);
    }

    [Fact]
    public async Task Handle_DeniesNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var (handler, _) = CreateHandler(stranger.Id, notebook);

        var result = await handler.Handle(new ListTrashedPagesQuery(notebook.Slug, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_DeniesAnonymous()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = new ListTrashedPagesQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository { notebook },
            new StubPageRepository()
        );

        var result = await handler.Handle(new ListTrashedPagesQuery(notebook.Slug, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_PaginatesRoots()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var pages = new StubPageRepository();
        for (var i = 0; i < 3; i++)
        {
            pages.Add(Trashed(notebook.Id, null, $"Page {i}", $"page-{i}", "a", DateTimeOffset.UtcNow.AddMinutes(-i)));
        }

        var notebooks = new StubNotebookRepository { notebook };
        var handler = CreateHandler(owner.Id, notebooks, pages);

        var first = await handler.Handle(new ListTrashedPagesQuery(notebook.Slug, 1, 2), CancellationToken.None);
        var second = await handler.Handle(new ListTrashedPagesQuery(notebook.Slug, 2, 2), CancellationToken.None);

        Assert.Equal(2, first.Value!.Items.Count);
        Assert.True(first.Value.HasNextPage);
        Assert.Single(second.Value!.Items);
        Assert.Equal(3, first.Value.TotalCount);
    }

    private static Page Trashed(Guid notebookId, Guid? parentId, string title, string slug, string sortKey, DateTimeOffset deletedAtUtc)
    {
        var page = Page.Create(notebookId, parentId, title, slug, sortKey);
        page.SoftDelete(deletedAtUtc);
        return page;
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (ListTrashedPagesQueryHandler Handler, StubPageRepository Pages) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        return (CreateHandler(currentUserId, new StubNotebookRepository { notebook }, pages), pages);
    }

    private static ListTrashedPagesQueryHandler CreateHandler(
        Guid currentUserId,
        StubNotebookRepository notebooks,
        StubPageRepository pages
    ) => new(new StubCurrentUserAccessor(new CurrentUser(currentUserId)), notebooks, pages);
}
