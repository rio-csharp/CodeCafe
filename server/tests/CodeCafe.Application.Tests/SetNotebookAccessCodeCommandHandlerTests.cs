using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.SetNotebookAccessCode;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class SetNotebookAccessCodeCommandHandlerTests
{
    [Fact]
    public async Task Handle_StoresHashOnly()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner, notebook);

        var result = await handler.Handle(
            new SetNotebookAccessCodeCommand(notebook.Slug, "secret-code"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed:secret-code", notebook.AccessCodeHash);
        Assert.NotEqual("secret-code", notebook.AccessCodeHash);
    }

    [Fact]
    public async Task Handle_ClearsAccessCode_WithNull()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        notebook.SetAccessCodeHash("hashed:old-code");
        var handler = CreateHandler(owner, notebook);

        var result = await handler.Handle(
            new SetNotebookAccessCodeCommand(notebook.Slug, null),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Null(notebook.AccessCodeHash);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Title", null, "some-slug", NotebookVisibility.Private);

    private static SetNotebookAccessCodeCommandHandler CreateHandler(User currentUser, Notebook notebook)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)),
            new StubNotebookRepository { notebook },
            new StubUnitOfWork(),
            new StubPasswordHasher()
        );
}
