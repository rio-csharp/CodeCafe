using CodeCafe.Application.Auth;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Trash.PurgeTrashedNotebooks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class PurgeTrashedNotebooksCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesOnlyOwnTrashedNotebooks()
    {
        var owner = SeedOwner();
        var other = User.Create("other@example.com", "other@example.com", "Other", "hash");
        var trashed = SeedNotebook(owner, "trashed");
        var foreignTrashed = SeedNotebook(other, "foreign");
        trashed.SoftDelete(DateTimeOffset.UtcNow);
        foreignTrashed.SoftDelete(DateTimeOffset.UtcNow);
        var live = SeedNotebook(owner, "live");
        var notebooks = new StubNotebookRepository { trashed, live, foreignTrashed };
        var handler = CreateHandler(owner.Id, notebooks);

        var result = await handler.Handle(new PurgeTrashedNotebooksCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(trashed, notebooks);
        Assert.Contains(live, notebooks);
        Assert.Contains(foreignTrashed, notebooks);
    }

    [Fact]
    public async Task Handle_Anonymous_GetsUserNotFound()
    {
        var owner = SeedOwner();
        var trashed = SeedNotebook(owner, "trashed");
        trashed.SoftDelete(DateTimeOffset.UtcNow);
        var notebooks = new StubNotebookRepository { trashed };
        var handler = new PurgeTrashedNotebooksCommandHandler(new StubCurrentUserAccessor(null), notebooks, new StubUnitOfWork());

        var result = await handler.Handle(new PurgeTrashedNotebooksCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
        Assert.Contains(trashed, notebooks);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, "Notebook", null, slug, NotebookVisibility.Private);

    private static PurgeTrashedNotebooksCommandHandler CreateHandler(Guid currentUserId, StubNotebookRepository notebooks)
        => new(new StubCurrentUserAccessor(new CurrentUser(currentUserId)), notebooks, new StubUnitOfWork());
}
