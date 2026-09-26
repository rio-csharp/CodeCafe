using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests;

public sealed class GetNotebookDetailsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPublicNotebook_Anonymously()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, NotebookVisibility.Public);
        var handler = CreateHandler(null, notebook);

        var result = await handler.Handle(new GetNotebookDetailsQuery(notebook.Slug), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(notebook.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_ReturnsPrivateNotebook_ForOwner()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, NotebookVisibility.Private);
        var handler = CreateHandler(new CurrentUser(owner.Id), notebook);

        var result = await handler.Handle(new GetNotebookDetailsQuery(notebook.Id.ToString()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(notebook.Slug, result.Value!.Slug);
    }

    [Fact]
    public async Task Handle_ReturnsPrivateNotebook_ForSharedUser()
    {
        var owner = SeedOwner();
        var shared = User.Create("shared@example.com", "shared@example.com", "Shared", "hash");
        var notebook = SeedNotebook(owner, NotebookVisibility.Private);
        notebook.Share(shared.Id, CollaboratorRole.Viewer);
        var handler = CreateHandler(new CurrentUser(shared.Id), notebook);

        var result = await handler.Handle(new GetNotebookDetailsQuery(notebook.Slug), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_ForPrivateNotebook_WhenStranger()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, NotebookVisibility.Private);
        var handler = CreateHandler(null, notebook);

        var result = await handler.Handle(new GetNotebookDetailsQuery(notebook.Slug), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_RequiresAccessCode_ForUnlistedNotebookWithCode_WhenStranger()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, NotebookVisibility.Unlisted);
        notebook.SetAccessCodeHash("hashed:s3cret");
        var handler = CreateHandler(null, notebook);

        var missing = await handler.Handle(new GetNotebookDetailsQuery(notebook.Slug), CancellationToken.None);
        var wrong = await handler.Handle(
            new GetNotebookDetailsQuery(notebook.Slug, "wrong-code"),
            CancellationToken.None
        );

        Assert.False(missing.IsSuccess);
        Assert.Equal(NotebookErrors.AccessCodeRequired, missing.Error);
        Assert.False(wrong.IsSuccess);
        Assert.Equal(NotebookErrors.AccessCodeRequired, wrong.Error);
    }

    [Fact]
    public async Task Handle_ReturnsUnlistedNotebookWithCode_WhenAccessCodeMatches()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, NotebookVisibility.Unlisted);
        notebook.SetAccessCodeHash("hashed:s3cret");
        var handler = CreateHandler(null, notebook);

        var result = await handler.Handle(
            new GetNotebookDetailsQuery(notebook.Slug, "s3cret"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(notebook.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_BypassesAccessCode_ForOwnerAndSharedUser()
    {
        var owner = SeedOwner();
        var shared = User.Create("shared@example.com", "shared@example.com", "Shared", "hash");
        var notebook = SeedNotebook(owner, NotebookVisibility.Unlisted);
        notebook.SetAccessCodeHash("hashed:s3cret");
        notebook.Share(shared.Id, CollaboratorRole.Viewer);

        var ownerResult = await CreateHandler(new CurrentUser(owner.Id), notebook)
            .Handle(new GetNotebookDetailsQuery(notebook.Slug), CancellationToken.None);
        var sharedResult = await CreateHandler(new CurrentUser(shared.Id), notebook)
            .Handle(new GetNotebookDetailsQuery(notebook.Slug), CancellationToken.None);

        Assert.True(ownerResult.IsSuccess);
        Assert.True(sharedResult.IsSuccess);
    }

    [Fact]
    public async Task Handle_IgnoresAccessCode_ForPublicNotebook()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, NotebookVisibility.Public);
        notebook.SetAccessCodeHash("hashed:s3cret");
        var handler = CreateHandler(null, notebook);

        var result = await handler.Handle(new GetNotebookDetailsQuery(notebook.Slug), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AccessCodeDoesNotUnlockPrivateNotebook()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, NotebookVisibility.Private);
        notebook.SetAccessCodeHash("hashed:s3cret");
        var handler = CreateHandler(null, notebook);

        var result = await handler.Handle(
            new GetNotebookDetailsQuery(notebook.Slug, "s3cret"),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, NotebookVisibility visibility)
        => Notebook.Create(owner.Id, "Title", null, "some-slug", visibility);

    private static GetNotebookDetailsQueryHandler CreateHandler(CurrentUser? currentUser, Notebook notebook)
        => new(
            new StubCurrentUserAccessor(currentUser),
            new StubUserRepository(),
            new StubNotebookRepository { notebook },
            new StubPageRepository(),
            new StubPasswordHasher()
        );
}
