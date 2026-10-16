using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.DeleteNotebook;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class DeleteNotebookCommandHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesNotebook()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, notebook, unitOfWork);

        var result = await handler.Handle(new DeleteNotebookCommand(notebook.Slug), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(notebook.DeletedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_ForNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(stranger, notebook, unitOfWork);

        var result = await handler.Handle(new DeleteNotebookCommand(notebook.Slug), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Null(notebook.DeletedAtUtc);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Title", null, "some-slug", NotebookVisibility.Private);

    private static DeleteNotebookCommandHandler CreateHandler(User currentUser, Notebook notebook, StubUnitOfWork unitOfWork)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            new StubNotebookRepository { notebook },
            unitOfWork
        );
}
