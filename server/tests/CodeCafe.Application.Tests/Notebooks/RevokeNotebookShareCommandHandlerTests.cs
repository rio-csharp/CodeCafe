using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.RevokeNotebookShare;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class RevokeNotebookShareCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesShare()
    {
        var owner = SeedOwner();
        var target = User.Create("target@example.com", "target@example.com", "Target", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(target.Id, CollaboratorRole.Viewer);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, notebook, unitOfWork);

        var result = await handler.Handle(
            new RevokeNotebookShareCommand(notebook.Slug, target.Id),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(notebook.Shares);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_IsIdempotent_WhenShareDoesNotExist()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner, notebook, new StubUnitOfWork());

        var result = await handler.Handle(
            new RevokeNotebookShareCommand(notebook.Slug, Guid.CreateVersion7()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(notebook.Shares);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Title", null, "some-slug", NotebookVisibility.Private);

    private static RevokeNotebookShareCommandHandler CreateHandler(User currentUser, Notebook notebook, StubUnitOfWork unitOfWork)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            new StubNotebookRepository { notebook },
            unitOfWork
        );
}
