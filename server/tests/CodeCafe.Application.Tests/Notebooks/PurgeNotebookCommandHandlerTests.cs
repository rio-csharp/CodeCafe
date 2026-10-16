using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Trash.PurgeNotebook;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class PurgeNotebookCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesTrashedNotebook_ForOwner()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        var (handler, notebooks, unitOfWork) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(new PurgeNotebookCommand(notebook.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(notebooks);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_LiveNotebook_IsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, notebooks, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(new PurgeNotebookCommand(notebook.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Single(notebooks);
    }

    [Fact]
    public async Task Handle_DeniesNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        var (handler, notebooks, _) = CreateHandler(stranger.Id, notebook);

        var result = await handler.Handle(new PurgeNotebookCommand(notebook.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Single(notebooks);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (PurgeNotebookCommandHandler Handler, StubNotebookRepository Notebooks, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        params Notebook[] seededNotebooks
    )
    {
        var notebooks = new StubNotebookRepository();
        notebooks.AddRange(seededNotebooks);
        var unitOfWork = new StubUnitOfWork();
        return (
            new PurgeNotebookCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                notebooks,
                unitOfWork
            ),
            notebooks,
            unitOfWork
        );
    }
}
