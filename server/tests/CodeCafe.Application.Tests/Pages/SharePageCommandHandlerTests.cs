using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.SharePage;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class SharePageCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsShareForTargetUser()
    {
        var owner = SeedOwner();
        var target = User.Create("target@example.com", "target@example.com", "Target", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook, page, target);

        var result = await handler.Handle(
            new SharePageCommand(page.Id, "  Target@Example.COM ", CollaboratorRole.Viewer),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var share = Assert.Single(page.Shares);
        Assert.Equal(target.Id, share.UserId);
        Assert.Equal(CollaboratorRole.Viewer, share.Role);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_UpdatesRole_WhenAlreadyShared()
    {
        var owner = SeedOwner();
        var target = User.Create("target@example.com", "target@example.com", "Target", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        page.Share(target.Id, CollaboratorRole.Viewer);
        var (handler, _) = CreateHandler(owner.Id, notebook, page, target);

        var result = await handler.Handle(
            new SharePageCommand(page.Id, "target@example.com", CollaboratorRole.Editor),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var share = Assert.Single(page.Shares);
        Assert.Equal(CollaboratorRole.Editor, share.Role);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenEmailIsUnknown()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new SharePageCommand(page.Id, "ghost@example.com", CollaboratorRole.Viewer),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.ShareTargetNotFound, result.Error);
        Assert.Empty(page.Shares);
    }

    [Fact]
    public async Task Handle_ReturnsValidation_WhenSharingWithOwner()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, _) = CreateHandler(owner.Id, notebook, page, owner);

        var result = await handler.Handle(
            new SharePageCommand(page.Id, "owner@example.com", CollaboratorRole.Viewer),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.CannotShareWithOwner, result.Error);
        Assert.Empty(page.Shares);
    }

    [Fact]
    public async Task Handle_DeniesNonOwner_EvenNotebookEditor()
    {
        var owner = SeedOwner();
        var editor = User.Create("editor@example.com", "editor@example.com", "Editor", "hash");
        var target = User.Create("target@example.com", "target@example.com", "Target", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(editor.Id, CollaboratorRole.Editor);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, _) = CreateHandler(editor.Id, notebook, page, target);

        var result = await handler.Handle(
            new SharePageCommand(page.Id, "target@example.com", CollaboratorRole.Viewer),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Empty(page.Shares);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (SharePageCommandHandler Handler, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page page,
        params User[] otherUsers
    )
    {
        var users = new StubUserRepository();
        users.AddRange(otherUsers);
        var unitOfWork = new StubUnitOfWork();
        return (
            new SharePageCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                users,
                new StubNotebookRepository { notebook },
                new StubPageRepository { page },
                unitOfWork
            ),
            unitOfWork
        );
    }
}
