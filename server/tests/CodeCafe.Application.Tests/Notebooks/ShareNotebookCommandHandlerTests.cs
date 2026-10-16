using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.ShareNotebook;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class ShareNotebookCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsShareForTargetUser()
    {
        var owner = SeedOwner();
        var target = User.Create("target@example.com", "target@example.com", "Target", "hash");
        var notebook = SeedNotebook(owner);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, notebook, unitOfWork, target);

        var result = await handler.Handle(
            new ShareNotebookCommand(notebook.Slug, "  Target@Example.COM ", CollaboratorRole.Viewer),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var share = Assert.Single(notebook.Shares);
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
        notebook.Share(target.Id, CollaboratorRole.Viewer);
        var handler = CreateHandler(owner, notebook, new StubUnitOfWork(), target);

        var result = await handler.Handle(
            new ShareNotebookCommand(notebook.Slug, "target@example.com", CollaboratorRole.Editor),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var share = Assert.Single(notebook.Shares);
        Assert.Equal(CollaboratorRole.Editor, share.Role);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenEmailIsUnknown()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner, notebook, new StubUnitOfWork());

        var result = await handler.Handle(
            new ShareNotebookCommand(notebook.Slug, "ghost@example.com", CollaboratorRole.Viewer),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.ShareTargetNotFound, result.Error);
        Assert.Empty(notebook.Shares);
    }

    [Fact]
    public async Task Handle_ReturnsValidation_WhenSharingWithOwner()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner, notebook, new StubUnitOfWork(), owner);

        var result = await handler.Handle(
            new ShareNotebookCommand(notebook.Slug, "owner@example.com", CollaboratorRole.Viewer),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.CannotShareWithOwner, result.Error);
        Assert.Empty(notebook.Shares);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Title", null, "some-slug", NotebookVisibility.Private);

    private static ShareNotebookCommandHandler CreateHandler(
        User currentUser,
        Notebook notebook,
        StubUnitOfWork unitOfWork,
        params User[] otherUsers
    )
    {
        var users = new StubUserRepository { currentUser };
        users.AddRange(otherUsers);
        return new ShareNotebookCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            users,
            new StubNotebookRepository { notebook },
            unitOfWork
        );
    }
}
