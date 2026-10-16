using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.UpdateNotebook;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class UpdateNotebookCommandHandlerTests
{
    [Fact]
    public async Task Handle_AppliesProvidedFields_AndKeepsTheRest()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, notebook, unitOfWork);

        var result = await handler.Handle(
            new UpdateNotebookCommand(notebook.Slug, "  New Title  ", null, null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", notebook.Title);
        Assert.Equal("Original description", notebook.Description);
        Assert.Equal(NotebookVisibility.Private, notebook.Visibility);
        Assert.Equal("New Title", result.Value!.Title);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ClearsDescription_WhenBlankProvided()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner, notebook, new StubUnitOfWork());

        var result = await handler.Handle(
            new UpdateNotebookCommand(notebook.Slug, null, "   ", NotebookVisibility.Public),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Null(notebook.Description);
        Assert.Equal(NotebookVisibility.Public, notebook.Visibility);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_ForNonOwner()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(stranger, notebook, unitOfWork);

        var result = await handler.Handle(
            new UpdateNotebookCommand(notebook.Slug, "Hijack", null, null),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Equal("Original title", notebook.Title);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Original title", "Original description", "original-slug", NotebookVisibility.Private);

    private static UpdateNotebookCommandHandler CreateHandler(User currentUser, Notebook notebook, StubUnitOfWork unitOfWork)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            new StubUserRepository { currentUser },
            new StubNotebookRepository { notebook },
            unitOfWork
        );
}
