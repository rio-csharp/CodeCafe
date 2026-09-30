using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.DeletePage;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Pages;

public sealed class DeletePageCommandHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesTheWholeSubtree_AndUnlinksTheRoot()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var b = Page.Create(notebook.Id, null, "B", "b", "b");
        a.SetNextSibling(b.Id);
        var child = Page.Create(notebook.Id, b.Id, "Child", "child", "a");
        var grandchild = Page.Create(notebook.Id, child.Id, "Grandchild", "grandchild", "a");
        var (handler, _) = CreateHandler(owner.Id, notebook, a, b, child, grandchild);

        var result = await handler.Handle(new DeletePageCommand(b.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(b.DeletedAtUtc);
        Assert.NotNull(child.DeletedAtUtc);
        Assert.NotNull(grandchild.DeletedAtUtc);
        Assert.Null(a.DeletedAtUtc);
        Assert.Null(a.NextSiblingId); // the live chain skips the trashed subtree root
    }

    [Fact]
    public async Task Handle_DeletingFirstChild_RelinksTheParent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var first = Page.Create(notebook.Id, parent.Id, "First", "first", "a");
        var second = Page.Create(notebook.Id, parent.Id, "Second", "second", "b");
        parent.SetFirstChild(first.Id);
        first.SetNextSibling(second.Id);
        var (handler, _) = CreateHandler(owner.Id, notebook, parent, first, second);

        var result = await handler.Handle(new DeletePageCommand(first.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(second.Id, parent.FirstChildId); // the parent heads the child chain
    }

    [Fact]
    public async Task Handle_DeniesStranger()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var a = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, _) = CreateHandler(stranger.Id, notebook, a);

        var result = await handler.Handle(new DeletePageCommand(a.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Null(a.DeletedAtUtc);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (DeletePageCommandHandler Handler, StubPageRepository Pages) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        return (
            new DeletePageCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                new StubUnitOfWork()
            ),
            pages
        );
    }
}
