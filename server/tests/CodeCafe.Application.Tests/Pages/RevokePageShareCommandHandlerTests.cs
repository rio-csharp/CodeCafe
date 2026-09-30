using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.RevokePageShare;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class RevokePageShareCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesShare()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        page.Share(guest.Id, CollaboratorRole.Viewer);
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new RevokePageShareCommand(page.Id, guest.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(page.Shares);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_RevokingAnAbsentShare_IsANoOp()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new RevokePageShareCommand(page.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(page.Shares);
    }

    [Fact]
    public async Task Handle_DeniesNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        page.Share(guest.Id, CollaboratorRole.Viewer);
        var (handler, _) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(new RevokePageShareCommand(page.Id, guest.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Single(page.Shares); // the share survives the rejected attempt
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (RevokePageShareCommandHandler Handler, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page page
    )
    {
        var unitOfWork = new StubUnitOfWork();
        return (
            new RevokePageShareCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                new StubPageRepository { page },
                unitOfWork
            ),
            unitOfWork
        );
    }
}
