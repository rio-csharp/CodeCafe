using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Revisions.ListBlockRevisions;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests.Revisions;

public sealed class ListBlockRevisionsQueryHandlerTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ReturnsNewestFirst_WithKeysetPagination()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var blockId = Guid.CreateVersion7();
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, blockId, 1, T1),
            Row(page, blockId, 2, T1.AddHours(1)),
            Row(page, blockId, 3, T1.AddHours(2)),
            Row(page, Guid.CreateVersion7(), 1, T1.AddHours(3)), // another block: invisible
        };
        var handler = CreateHandler(owner.Id, notebook, page, revisions);

        var first = await handler.Handle(new ListBlockRevisionsQuery(page.Id, blockId, null, 2), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal([3L, 2L], first.Value!.Items.Select(item => item.BlockVersion).ToArray());
        Assert.NotNull(first.Value.NextCursor);

        var second = await handler.Handle(
            new ListBlockRevisionsQuery(page.Id, blockId, first.Value.NextCursor, 2),
            CancellationToken.None
        );

        Assert.True(second.IsSuccess);
        Assert.Equal([1L], second.Value!.Items.Select(item => item.BlockVersion).ToArray());
        Assert.Null(second.Value.NextCursor);
    }

    [Fact]
    public async Task Handle_InvalidCursor_ReturnsValidationError()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var handler = CreateHandler(owner.Id, notebook, page, new StubBlockRevisionRepository());

        var result = await handler.Handle(
            new ListBlockRevisionsQuery(page.Id, Guid.CreateVersion7(), "not-a-cursor", null),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.InvalidCursor, result.Error);
    }

    [Fact]
    public async Task Handle_Anonymous_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var revisions = new StubBlockRevisionRepository();
        var handler = new ListBlockRevisionsQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubPasswordHasher(),
            revisions
        );

        var result = await handler.Handle(
            new ListBlockRevisionsQuery(page.Id, Guid.CreateVersion7(), null, null),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static BlockRevision Row(Page page, Guid blockId, long blockVersion, DateTimeOffset atUtc)
        => BlockRevision.Record(
            page.Id,
            blockId,
            Guid.CreateVersion7(),
            blockVersion,
            BlockChangeKind.Updated,
            RevisionSource.Human,
            null,
            "a",
            "paragraph",
            """{"spans":[]}""",
            string.Empty,
            subtreeJson: null,
            atUtc: atUtc
        );

    private static ListBlockRevisionsQueryHandler CreateHandler(
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
