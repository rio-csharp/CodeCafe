using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.SetNotebookFavorite;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests;

public sealed class SetNotebookFavoriteCommandHandlerTests
{
    [Fact]
    public async Task Handle_TogglesFavorite_ForOwner()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var notebooks = new StubNotebookRepository { notebook };
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner.Id, notebooks, unitOfWork);

        var result = await handler.Handle(
            new SetNotebookFavoriteCommand(notebook.Slug, true),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Contains((notebook.Id, owner.Id), notebooks.Favorites);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_FavoritesArePerUser()
    {
        var owner = SeedOwner();
        var other = User.Create("other@example.com", "other@example.com", "Other", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(other.Id, CollaboratorRole.Viewer);
        var notebooks = new StubNotebookRepository { notebook };

        await CreateHandler(owner.Id, notebooks, new StubUnitOfWork())
            .Handle(new SetNotebookFavoriteCommand(notebook.Slug, true), CancellationToken.None);
        var result = await CreateHandler(other.Id, notebooks, new StubUnitOfWork())
            .Handle(new SetNotebookFavoriteCommand(notebook.Slug, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains((notebook.Id, owner.Id), notebooks.Favorites);
        Assert.DoesNotContain((notebook.Id, other.Id), notebooks.Favorites);
    }

    [Fact]
    public async Task Handle_LetsCollaboratorFavorite()
    {
        var owner = SeedOwner();
        var collaborator = User.Create("collab@example.com", "collab@example.com", "Collab", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(collaborator.Id, CollaboratorRole.Viewer);
        var notebooks = new StubNotebookRepository { notebook };
        var handler = CreateHandler(collaborator.Id, notebooks, new StubUnitOfWork());

        var result = await handler.Handle(
            new SetNotebookFavoriteCommand(notebook.Slug, true),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Contains((notebook.Id, collaborator.Id), notebooks.Favorites);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenNotebookNotVisibleToCaller()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var notebooks = new StubNotebookRepository { notebook };
        var handler = CreateHandler(stranger.Id, notebooks, new StubUnitOfWork());

        var result = await handler.Handle(
            new SetNotebookFavoriteCommand(notebook.Slug, true),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Empty(notebooks.Favorites);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Title", null, "some-slug", NotebookVisibility.Private);

    private static SetNotebookFavoriteCommandHandler CreateHandler(
        Guid currentUserId,
        StubNotebookRepository notebooks,
        StubUnitOfWork unitOfWork
    ) => new(new StubCurrentUserAccessor(new CurrentUser(currentUserId)), notebooks, unitOfWork);
}
