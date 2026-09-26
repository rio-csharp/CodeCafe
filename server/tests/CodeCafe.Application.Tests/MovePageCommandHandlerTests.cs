using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.MovePage;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests;

public sealed class MovePageCommandHandlerTests
{
    [Fact]
    public async Task Handle_MovesToAnotherParent_AndFixesBothChains()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        // Roots: a -> b; parent p with child c
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var b = Page.Create(notebook.Id, null, "B", "b", "b");
        a.SetNextSibling(b.Id);
        var p = Page.Create(notebook.Id, null, "P", "p", "p");
        var c = Page.Create(notebook.Id, p.Id, "C", "c", "a");
        var (handler, _) = CreateHandler(owner.Id, notebook, a, b, p, c);

        // Move b under p, after c.
        var result = await handler.Handle(
            new MovePageCommand(b.Id, "/p", c.Id),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(p.Id, b.ParentId);
        Assert.Null(a.NextSiblingId); // b was unlinked from the root chain
        Assert.Equal(b.Id, c.NextSiblingId); // and spliced after c
        Assert.Null(b.NextSiblingId);
        Assert.True(string.CompareOrdinal(c.SortKey, b.SortKey) < 0);
        Assert.Equal("/p/b", result.Value!.Path);
    }

    [Fact]
    public async Task Handle_ReordersWithinSameParent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var b = Page.Create(notebook.Id, null, "B", "b", "b");
        var c = Page.Create(notebook.Id, null, "C", "c", "c");
        a.SetNextSibling(b.Id);
        b.SetNextSibling(c.Id);
        var (handler, _) = CreateHandler(owner.Id, notebook, a, b, c);

        // Move c between a and b.
        var result = await handler.Handle(new MovePageCommand(c.Id, null, a.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(b.NextSiblingId); // c left the tail
        Assert.Equal(c.Id, a.NextSiblingId);
        Assert.Equal(b.Id, c.NextSiblingId);
        Assert.True(string.CompareOrdinal(a.SortKey, c.SortKey) < 0);
        Assert.True(string.CompareOrdinal(c.SortKey, b.SortKey) < 0);
    }

    [Fact]
    public async Task Handle_MovingFirstChild_RelinksTheOldParent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var p = Page.Create(notebook.Id, null, "P", "p", "a");
        var x = Page.Create(notebook.Id, null, "X", "x", "b");
        var c1 = Page.Create(notebook.Id, p.Id, "C1", "c1", "a");
        var c2 = Page.Create(notebook.Id, p.Id, "C2", "c2", "b");
        p.SetFirstChild(c1.Id);
        c1.SetNextSibling(c2.Id);
        var (handler, _) = CreateHandler(owner.Id, notebook, p, x, c1, c2);

        // Move the first child to the notebook root (appended after x).
        var result = await handler.Handle(new MovePageCommand(c1.Id, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(c2.Id, p.FirstChildId); // the parent heads the remaining child chain
        Assert.Equal(c1.Id, x.NextSiblingId);
        Assert.Null(c1.ParentId);
        Assert.Null(c1.NextSiblingId);
    }

    [Fact]
    public async Task Handle_MovingIntoChildlessParent_SetsParentAsChainHead()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var p = Page.Create(notebook.Id, null, "P", "p", "a");
        var x = Page.Create(notebook.Id, null, "X", "x", "b");
        var (handler, _) = CreateHandler(owner.Id, notebook, p, x);

        var result = await handler.Handle(new MovePageCommand(x.Id, "/p", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(x.Id, p.FirstChildId);
        Assert.Equal(p.Id, x.ParentId);
    }

    [Fact]
    public async Task Handle_RebalancesTheLevel_WhenTheTightKeyWouldGrowTooLong()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        // A key at the 64-char cap leaves no room after b, forcing a rebalance of the level.
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var c = Page.Create(notebook.Id, null, "C", "c", "c");
        var b = Page.Create(notebook.Id, null, "B", "b", new string('z', 60));
        a.SetNextSibling(c.Id);
        c.SetNextSibling(b.Id);
        var (handler, _) = CreateHandler(owner.Id, notebook, a, c, b);

        // Move c after b: appending past the capped key triggers the rebalance.
        var result = await handler.Handle(new MovePageCommand(c.Id, null, b.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(new[] { a, b, c }, page => Assert.InRange(page.SortKey.Length, 1, 2));
        Assert.True(string.CompareOrdinal(a.SortKey, b.SortKey) < 0);
        Assert.True(string.CompareOrdinal(b.SortKey, c.SortKey) < 0);
        Assert.Equal(b.Id, a.NextSiblingId);
        Assert.Equal(c.Id, b.NextSiblingId);
        Assert.Null(c.NextSiblingId);
    }

    [Fact]
    public async Task Handle_MovingAPageWithChildren_PreservesItsChildChain()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        // Roots: a -> p -> x; p heads its own child chain (c).
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var p = Page.Create(notebook.Id, null, "P", "p", "m");
        var x = Page.Create(notebook.Id, null, "X", "x", "z");
        var c = Page.Create(notebook.Id, p.Id, "C", "c", "a");
        a.SetNextSibling(p.Id);
        p.SetNextSibling(x.Id);
        p.SetFirstChild(c.Id);
        var (handler, _) = CreateHandler(owner.Id, notebook, a, p, x, c);

        var result = await handler.Handle(new MovePageCommand(p.Id, null, x.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(x.Id, a.NextSiblingId); // the old chain skips p
        Assert.Null(p.NextSiblingId); // p is the new tail
        Assert.Equal(c.Id, p.FirstChildId); // the child chain moves with p, untouched
    }

    [Fact]
    public async Task Handle_MovingTheRootChainHead_ReheadsTheNotebook()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var b = Page.Create(notebook.Id, null, "B", "b", "b");
        a.SetNextSibling(b.Id);
        notebook.SetFirstPage(a.Id);
        var (handler, _) = CreateHandler(owner.Id, notebook, a, b);

        var result = await handler.Handle(new MovePageCommand(a.Id, null, b.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(b.Id, notebook.FirstPageId);
        Assert.Null(a.NextSiblingId);
    }

    [Fact]
    public async Task Handle_RejectsMoveIntoOwnDescendant()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var child = Page.Create(notebook.Id, parent.Id, "Child", "child", "a");
        var (handler, _) = CreateHandler(owner.Id, notebook, parent, child);

        var result = await handler.Handle(
            new MovePageCommand(parent.Id, "/parent/child", null),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.CannotMoveIntoDescendant, result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsAfterPageNotFound_WhenTargetIsNotASiblingOfTheNewParent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var b = Page.Create(notebook.Id, null, "B", "b", "b");
        var p = Page.Create(notebook.Id, null, "P", "p", "p");
        var (handler, _) = CreateHandler(owner.Id, notebook, a, b, p);

        // b is not a child of p.
        var result = await handler.Handle(new MovePageCommand(a.Id, "/p", b.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.AfterPageNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_DeniesStranger()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, _) = CreateHandler(stranger.Id, notebook, a);

        var result = await handler.Handle(new MovePageCommand(a.Id, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (MovePageCommandHandler Handler, StubPageRepository Pages) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        return (
            new MovePageCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                new StubUserRepository(),
                new StubUnitOfWork()
            ),
            pages
        );
    }
}
