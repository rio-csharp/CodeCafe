using CodeCafe.Application.Auth;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Trash.ListTrash;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests;

public sealed class ListTrashQueryHandlerTests
{
    [Fact]
    public async Task Handle_ListsOnlyOwnTrashedNotebooks_WithPageCounts()
    {
        var owner = SeedOwner();
        var other = User.Create("other@example.com", "other@example.com", "Other", "hash");
        var trashedWithPages = SeedNotebook(owner, "trashed-a");
        var trashedEmpty = SeedNotebook(owner, "trashed-b");
        var live = SeedNotebook(owner, "live");
        var foreignTrashed = SeedNotebook(other, "foreign");
        trashedWithPages.SoftDelete(DateTimeOffset.UtcNow);
        trashedEmpty.SoftDelete(DateTimeOffset.UtcNow);
        foreignTrashed.SoftDelete(DateTimeOffset.UtcNow);

        var pages = new StubPageRepository
        {
            Page.Create(trashedWithPages.Id, null, "One", "one", "a"),
            Page.Create(trashedWithPages.Id, null, "Two", "two", "b"),
            Page.Create(live.Id, null, "Live", "live", "a"), // live notebooks do not count here
        };
        var notebooks = new StubNotebookRepository { trashedWithPages, trashedEmpty, live, foreignTrashed };
        var handler = CreateHandler(owner.Id, notebooks, pages);

        var result = await handler.Handle(new ListTrashQuery(null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        var entry = Assert.Single(result.Value.Items, item => item.NotebookId == trashedWithPages.Id);
        Assert.Equal(2, entry.PageCount);
        var empty = Assert.Single(result.Value.Items, item => item.NotebookId == trashedEmpty.Id);
        Assert.Equal(0, empty.PageCount);
    }

    [Fact]
    public async Task Handle_Paginates()
    {
        var owner = SeedOwner();
        var notebooks = new StubNotebookRepository();
        for (var i = 0; i < 3; i++)
        {
            var notebook = SeedNotebook(owner, $"trashed-{i}");
            notebook.SoftDelete(DateTimeOffset.UtcNow.AddMinutes(-i));
            notebooks.Add(notebook);
        }

        var handler = CreateHandler(owner.Id, notebooks, new StubPageRepository());

        var first = await handler.Handle(new ListTrashQuery(1, 2), CancellationToken.None);
        var second = await handler.Handle(new ListTrashQuery(2, 2), CancellationToken.None);

        Assert.Equal(2, first.Value!.Items.Count);
        Assert.True(first.Value.HasNextPage);
        Assert.Single(second.Value!.Items);
        Assert.False(second.Value.HasNextPage);
        Assert.Equal(3, first.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_Anonymous_GetsUserNotFound()
    {
        var handler = new ListTrashQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository(),
            new StubPageRepository()
        );

        var result = await handler.Handle(new ListTrashQuery(null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, "Notebook", null, slug, NotebookVisibility.Private);

    private static ListTrashQueryHandler CreateHandler(
        Guid currentUserId,
        StubNotebookRepository notebooks,
        StubPageRepository pages
    ) => new(new StubCurrentUserAccessor(new CurrentUser(currentUserId)), notebooks, pages);
}
