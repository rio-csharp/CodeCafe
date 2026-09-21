using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.SetNotebookTags;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class SetNotebookTagsCommandHandlerTests
{
    [Fact]
    public async Task Handle_NormalizesAndDeduplicatesTags()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(owner, notebook, unitOfWork);

        var result = await handler.Handle(
            new SetNotebookTagsCommand(notebook.Slug, ["  Foo ", "bar", "FOO", "  "]),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(["foo", "bar"], notebook.Tags);
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

        var result = await handler.Handle(
            new SetNotebookTagsCommand(notebook.Slug, ["tag"]),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Empty(notebook.Tags);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Title", null, "some-slug", NotebookVisibility.Private);

    private static SetNotebookTagsCommandHandler CreateHandler(User currentUser, Notebook notebook, StubUnitOfWork unitOfWork)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            new StubNotebookRepository { notebook },
            unitOfWork
        );
}
