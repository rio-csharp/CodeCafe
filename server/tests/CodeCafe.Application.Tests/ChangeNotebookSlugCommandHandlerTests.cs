using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.ChangeNotebookSlug;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class ChangeNotebookSlugCommandHandlerTests
{
    [Fact]
    public async Task Handle_ChangesSlug_AndNormalizes()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "old-slug");
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, [notebook], unitOfWork);

        var result = await handler.Handle(
            new ChangeNotebookSlugCommand("old-slug", "  New-Slug "),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("new-slug", notebook.Slug);
        Assert.Equal("new-slug", result.Value!.Slug);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenSlugIsTaken()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "old-slug");
        var other = SeedNotebook(owner, "taken-slug");
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, [notebook, other], unitOfWork);

        var result = await handler.Handle(
            new ChangeNotebookSlugCommand("old-slug", "taken-slug"),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.SlugAlreadyTaken, result.Error);
        Assert.Equal("old-slug", notebook.Slug);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_SucceedsWithoutSaving_WhenSlugIsUnchanged()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "same-slug");
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, [notebook], unitOfWork);

        var result = await handler.Handle(
            new ChangeNotebookSlugCommand("same-slug", "same-slug"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenTheSlugIsTakenByAConcurrentRename()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "old-slug");
        // The pre-check sees no conflict; the database rejects the write, exactly as it would for
        // a rename that raced with another one.
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_notebooks_Slug"));
        var handler = CreateHandler(owner, [notebook], unitOfWork);

        var result = await handler.Handle(
            new ChangeNotebookSlugCommand("old-slug", "new-slug"),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.SlugAlreadyTaken, result.Error);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, "Title", null, slug, NotebookVisibility.Private);

    private static ChangeNotebookSlugCommandHandler CreateHandler(
        User currentUser,
        IEnumerable<Notebook> notebooks,
        StubUnitOfWork unitOfWork
    )
    {
        var repository = new StubNotebookRepository();
        repository.AddRange(notebooks);
        return new ChangeNotebookSlugCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            new StubUserRepository { currentUser },
            repository,
            unitOfWork
        );
    }
}
