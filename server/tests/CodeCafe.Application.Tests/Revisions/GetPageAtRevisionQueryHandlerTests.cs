using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Revisions.GetPageAtRevision;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests.Revisions;

public sealed class GetPageAtRevisionQueryHandlerTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ReconstructsStateAtTheRequestedInstant()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var blockId = Guid.CreateVersion7();
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, blockId, T1, BlockChangeKind.Added, """{"spans":[{"text":"v1","marks":[]}]}""", sortKey: "a", version: 1),
            Row(page, blockId, T1.AddDays(1), BlockChangeKind.Updated, """{"spans":[{"text":"v2","marks":[]}]}""", sortKey: "a", version: 2),
            Row(page, blockId, T1.AddDays(2), BlockChangeKind.Updated, """{"spans":[{"text":"v3","marks":[]}]}""", sortKey: "a", version: 3),
        };
        var handler = CreateHandler(owner.Id, notebook, page, revisions);

        var result = await handler.Handle(
            new GetPageAtRevisionQuery(page.Id, T1.AddDays(1).AddHours(1)),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var snapshot = result.Value!;
        var block = Assert.Single(snapshot.Blocks);
        Assert.Equal(blockId, block.Id);
        Assert.Equal("v2", block.Content.GetProperty("spans")[0].GetProperty("text").GetString());
        Assert.Equal(2, block.Version);
    }

    [Fact]
    public async Task Handle_ExcludesBlocksDeletedBeforeTheInstant_AndKeepsOnesDeletedAfter()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var deletedBefore = Guid.CreateVersion7();
        var deletedAfter = Guid.CreateVersion7();
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, deletedBefore, T1, BlockChangeKind.Added, """{"spans":[]}""", sortKey: "a"),
            Row(page, deletedBefore, T1.AddDays(1), BlockChangeKind.Deleted, """{"spans":[]}""", sortKey: "a"),
            Row(page, deletedAfter, T1, BlockChangeKind.Added, """{"spans":[]}""", sortKey: "b"),
            Row(page, deletedAfter, T1.AddDays(3), BlockChangeKind.Deleted, """{"spans":[]}""", sortKey: "b"),
        };
        var handler = CreateHandler(owner.Id, notebook, page, revisions);

        var result = await handler.Handle(
            new GetPageAtRevisionQuery(page.Id, T1.AddDays(2)),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var block = Assert.Single(result.Value!.Blocks);
        Assert.Equal(deletedAfter, block.Id);
    }

    [Fact]
    public async Task Handle_OrdersBlocksBySortKeyOrdinal()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, Guid.CreateVersion7(), T1, BlockChangeKind.Added, """{"spans":[]}""", sortKey: "b"),
            Row(page, Guid.CreateVersion7(), T1, BlockChangeKind.Added, """{"spans":[]}""", sortKey: "Z"),
            Row(page, Guid.CreateVersion7(), T1, BlockChangeKind.Added, """{"spans":[]}""", sortKey: "a"),
        };
        var handler = CreateHandler(owner.Id, notebook, page, revisions);

        var result = await handler.Handle(new GetPageAtRevisionQuery(page.Id, T1.AddDays(1)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Ordinal, not locale: Z sorts before a.
        Assert.Equal(["Z", "a", "b"], result.Value!.Blocks.Select(block => block.SortKey).ToArray());
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForAnUnknownPage()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var handler = CreateHandler(owner.Id, notebook, page, new StubBlockRevisionRepository());

        var result = await handler.Handle(
            new GetPageAtRevisionQuery(Guid.CreateVersion7(), T1),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static BlockRevision Row(
        Page page,
        Guid blockId,
        DateTimeOffset atUtc,
        BlockChangeKind kind,
        string contentJson,
        string sortKey,
        long version = 1
    )
        => BlockRevision.Record(
            page.Id,
            blockId,
            Guid.CreateVersion7(),
            version,
            kind,
            RevisionSource.Human,
            null,
            sortKey,
            "paragraph",
            contentJson,
            string.Empty,
            subtreeJson: null,
            atUtc: atUtc
        );

    private static GetPageAtRevisionQueryHandler CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page page,
        StubBlockRevisionRepository revisions
    )
        => new(
            new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubPasswordHasher(),
            revisions
        );

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook) => Page.Create(notebook.Id, null, "Page", "page", "a");
}
