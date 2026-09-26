using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Trash.RestoreNotebookFromTrash;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class RestoreNotebookFromTrashCommandHandlerTests
{
    [Fact]
    public async Task Handle_Restores_ForOwner()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "my-notebook");
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(new RestoreNotebookFromTrashCommand(notebook.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(notebook.DeletedAtUtc);
        Assert.Equal("my-notebook", notebook.Slug);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_SlugTaken_RestoresWithFreshSlug()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "my-notebook");
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        // Someone took the slug while the notebook sat in the trash.
        var squatter = SeedNotebook(owner, "my-notebook");
        var (handler, _) = CreateHandler(owner.Id, notebook, squatter);

        var result = await handler.Handle(new RestoreNotebookFromTrashCommand(notebook.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(notebook.DeletedAtUtc);
        Assert.NotEqual("my-notebook", notebook.Slug);
        Assert.StartsWith("my-notebook-", notebook.Slug);
    }

    [Fact]
    public async Task Handle_DeniesNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner, "my-notebook");
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        var (handler, _) = CreateHandler(stranger.Id, notebook);

        var result = await handler.Handle(new RestoreNotebookFromTrashCommand(notebook.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.NotNull(notebook.DeletedAtUtc);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, "Notebook", null, slug, NotebookVisibility.Private);

    private static (RestoreNotebookFromTrashCommandHandler Handler, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        params Notebook[] seededNotebooks
    )
    {
        var notebooks = new StubNotebookRepository();
        notebooks.AddRange(seededNotebooks);
        var unitOfWork = new StubUnitOfWork();
        return (
            new RestoreNotebookFromTrashCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                notebooks,
                unitOfWork
            ),
            unitOfWork
        );
    }
}
