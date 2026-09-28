using CodeCafe.Application.Common;
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
        var (handler, unitOfWork) = CreateHandler(owner.Id, [notebook]);

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
        var (handler, _) = CreateHandler(owner.Id, [notebook, squatter]);

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
        var (handler, _) = CreateHandler(stranger.Id, [notebook]);

        var result = await handler.Handle(new RestoreNotebookFromTrashCommand(notebook.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.NotNull(notebook.DeletedAtUtc);
    }

    [Fact]
    public async Task Handle_RetriesWithAFreshSlug_WhenTheSaveLosesARace()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "my-notebook");
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        // A squatter keeps the base slug taken, so the retry has to draw a different candidate
        // instead of repeating the one that was just rejected.
        var squatter = SeedNotebook(owner, "my-notebook");
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_notebooks_Slug"))
        {
            FailuresRemaining = 1
        };
        var (handler, _) = CreateHandler(owner.Id, [notebook, squatter], unitOfWork);

        var result = await handler.Handle(new RestoreNotebookFromTrashCommand(notebook.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(notebook.DeletedAtUtc);
        Assert.StartsWith("my-notebook-", notebook.Slug);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_GivesUp_WhenEveryAttemptLosesTheSlugRace()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "my-notebook");
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        var squatter = SeedNotebook(owner, "my-notebook");
        // 3 mirrors RestoreNotebookFromTrashCommandHandler.MaxSaveAttempts.
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_notebooks_Slug"))
        {
            FailuresRemaining = 3
        };
        var (handler, _) = CreateHandler(owner.Id, [notebook, squatter], unitOfWork);

        var result = await handler.Handle(new RestoreNotebookFromTrashCommand(notebook.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.SlugAlreadyTaken, result.Error);
        Assert.Equal(3, unitOfWork.SaveChangesCallCount);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, "Notebook", null, slug, NotebookVisibility.Private);

    private static (RestoreNotebookFromTrashCommandHandler Handler, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook[] seededNotebooks,
        StubUnitOfWork? unitOfWork = null
    )
    {
        var notebooks = new StubNotebookRepository();
        notebooks.AddRange(seededNotebooks);
        unitOfWork ??= new StubUnitOfWork();
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
