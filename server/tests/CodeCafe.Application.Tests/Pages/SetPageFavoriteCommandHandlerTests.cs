using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.SetPageFavorite;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class SetPageFavoriteCommandHandlerTests
{
    [Fact]
    public async Task Handle_TogglesFavorite_ForOwner()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, pages, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new SetPageFavoriteCommand(page.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains((page.Id, owner.Id), pages.Favorites);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_LetsPageShareReaderFavorite()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner); // private, the guest only has a page share
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(guest.Id, CollaboratorRole.Viewer);
        var (handler, pages, _) = CreateHandler(guest.Id, notebook, page);

        var result = await handler.Handle(new SetPageFavoriteCommand(page.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains((page.Id, guest.Id), pages.Favorites);
    }

    [Fact]
    public async Task Handle_DeniesStranger()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, pages, _) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(new SetPageFavoriteCommand(page.Id, true), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Empty(pages.Favorites);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (SetPageFavoriteCommandHandler Handler, StubPageRepository Pages, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var unitOfWork = new StubUnitOfWork();
        return (
            new SetPageFavoriteCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                new StubPasswordHasher(),
                unitOfWork
            ),
            pages,
            unitOfWork
        );
    }
}
