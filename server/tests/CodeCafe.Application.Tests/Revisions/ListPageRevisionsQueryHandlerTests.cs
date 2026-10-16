using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Revisions.ListPageRevisions;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests.Revisions;

public sealed class ListPageRevisionsQueryHandlerTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_GroupsRowsIntoBatches_NewestFirst_RowsChronological()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var olderBatch = Guid.CreateVersion7();
        var newerBatch = Guid.CreateVersion7();
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, olderBatch, T1, BlockChangeKind.Added),
            Row(page, olderBatch, T1.AddMinutes(1), BlockChangeKind.Added),
            Row(page, newerBatch, T1.AddDays(1), BlockChangeKind.Updated),
        };
        var handler = CreateHandler(owner.Id, notebook, page, revisions);

        var result = await handler.Handle(new ListPageRevisionsQuery(page.Id, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        var newest = result.Value.Items[0];
        Assert.Equal(T1.AddDays(1), newest.AtUtc);
        Assert.Equal(RevisionSource.Human, newest.Source);
        Assert.Single(newest.Changes);
        var oldest = result.Value.Items[1];
        // Rows inside a group come back in chronological order.
        Assert.Equal(
            [BlockChangeKind.Added, BlockChangeKind.Added],
            oldest.Changes.Select(change => change.ChangeKind).ToArray()
        );
        Assert.True(oldest.Changes[0].CreatedAtUtc < oldest.Changes[1].CreatedAtUtc);
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task Handle_PaginatesByWholeBatches()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var batch1 = Guid.CreateVersion7();
        var batch2 = Guid.CreateVersion7();
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, batch1, T1, BlockChangeKind.Added),
            Row(page, batch1, T1.AddMinutes(1), BlockChangeKind.Added),
            Row(page, batch2, T1.AddDays(1), BlockChangeKind.Updated),
        };
        var handler = CreateHandler(owner.Id, notebook, page, revisions);

        var first = await handler.Handle(new ListPageRevisionsQuery(page.Id, null, 1), CancellationToken.None);

        Assert.True(first.IsSuccess);
        var firstGroup = Assert.Single(first.Value!.Items);
        Assert.Equal(T1.AddDays(1), firstGroup.AtUtc);
        Assert.NotNull(first.Value.NextCursor);

        var second = await handler.Handle(new ListPageRevisionsQuery(page.Id, first.Value.NextCursor, 1), CancellationToken.None);

        Assert.True(second.IsSuccess);
        var secondGroup = Assert.Single(second.Value!.Items);
        // The whole older batch arrives on one page: groups never split.
        Assert.Equal(2, secondGroup.Changes.Count);
        Assert.Null(second.Value.NextCursor);
    }

    [Fact]
    public async Task Handle_InvalidCursor_ReturnsValidationError()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var handler = CreateHandler(owner.Id, notebook, page, new StubBlockRevisionRepository());

        var result = await handler.Handle(new ListPageRevisionsQuery(page.Id, "not-a-cursor", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.InvalidCursor, result.Error);
    }

    private static BlockRevision Row(Page page, Guid batchId, DateTimeOffset atUtc, BlockChangeKind kind)
        => BlockRevision.Record(
            page.Id,
            Guid.CreateVersion7(),
            batchId,
            1,
            kind,
            RevisionSource.Human,
            null,
            "a",
            "paragraph",
            """{"spans":[]}""",
            string.Empty,
            subtreeJson: null,
            atUtc: atUtc
        );

    private static ListPageRevisionsQueryHandler CreateHandler(
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
