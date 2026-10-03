using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Revisions.RestorePageToRevision;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests.Revisions;

public sealed class RestorePageToRevisionCommandHandlerTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T3 = new(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T4 = new(2026, 1, 4, 0, 0, 0, TimeSpan.Zero);

    private const string V1Json = """{"spans":[{"text":"v1","marks":[]}]}""";
    private const string V2Json = """{"spans":[{"text":"v2","marks":[]}]}""";

    [Fact]
    public async Task Handle_RollsBackContent_Structure_AndMembership_WithOriginalIds()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        // History: t1 adds A and B, t2 updates A, t3 deletes B, t4 adds C. Live now: A@v2, C.
        var a = NewBlock(page, "a", V2Json, "v2");
        var b = NewBlock(page, "b", V1Json, "v1");
        var c = NewBlock(page, "c", V1Json, "v1");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(c, page, parent: null, after: a);
        a.UpdateContent(V2Json, "v2"); // live version matches the t2 update
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, a, 1, BlockChangeKind.Added, "a", V1Json, "v1", T1),
            Row(page, b, 1, BlockChangeKind.Added, "b", V1Json, "v1", T1),
            Row(page, a, 2, BlockChangeKind.Updated, "a", V2Json, "v2", T2),
            Row(page, b, 1, BlockChangeKind.Deleted, "b", V1Json, "v1", T3),
            Row(page, c, 1, BlockChangeKind.Added, "c", V1Json, "v1", T4),
        };
        var historyCount = revisions.Count;
        var (handler, blocks) = CreateHandler(owner.Id, notebook, page, revisions, [a, c]);

        var result = await handler.Handle(new RestorePageToRevisionCommand(page.Id, T1), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        // Membership: C is gone, B is back with its ORIGINAL id.
        Assert.Equal(new[] { a.Id, b.Id }.OrderBy(id => id), blocks.Select(block => block.Id).OrderBy(id => id));
        // Payload: A carries the t1 content again.
        Assert.Equal(V1Json, a.ContentJson);
        // Structure: top level is A -> B, rebuilt from the historical placements.
        Assert.Equal(a.Id, page.FirstBlockId);
        Assert.Equal(b.Id, a.NextSiblingId);
        Assert.Null(blocks.Single(block => block.Id == b.Id).NextSiblingId);
        // The restore itself is recorded as one new batch (history stays append-only).
        var restoreRows = revisions.Skip(historyCount).ToList();
        Assert.Single(restoreRows.Select(row => row.BatchId).Distinct());
        Assert.Contains(restoreRows, row => row.ChangeKind == BlockChangeKind.Deleted && row.BlockId == c.Id);
        Assert.Contains(restoreRows, row => row.ChangeKind == BlockChangeKind.Added && row.BlockId == b.Id);
        Assert.Contains(restoreRows, row => row.ChangeKind == BlockChangeKind.Updated && row.BlockId == a.Id);
    }

    [Fact]
    public async Task Handle_RestoringThePresent_IsANoOp()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a", V1Json, "v1");
        BlockChain.Link(a, page, parent: null, after: null);
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, a, 1, BlockChangeKind.Added, "a", V1Json, "v1", T1),
        };
        var historyCount = revisions.Count;
        var (handler, blocks) = CreateHandler(owner.Id, notebook, page, revisions, [a]);

        var result = await handler.Handle(new RestorePageToRevisionCommand(page.Id, T4), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(blocks);
        Assert.Equal(historyCount, revisions.Count); // nothing changed, nothing recorded
    }

    [Fact]
    public async Task Handle_BeforeAllHistory_ClearsThePage()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a", V1Json, "v1");
        BlockChain.Link(a, page, parent: null, after: null);
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, a, 1, BlockChangeKind.Added, "a", V1Json, "v1", T1),
        };
        var (handler, blocks) = CreateHandler(owner.Id, notebook, page, revisions, [a]);

        var result = await handler.Handle(new RestorePageToRevisionCommand(page.Id, DateTimeOffset.UnixEpoch), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(blocks);
        Assert.Null(page.FirstBlockId);
        Assert.Single(revisions, row => row.ChangeKind == BlockChangeKind.Deleted && row.BlockId == a.Id);
    }

    [Fact]
    public async Task Handle_DeniesNonWriter()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _) = CreateHandler(stranger.Id, notebook, page, new StubBlockRevisionRepository(), []);

        var result = await handler.Handle(new RestorePageToRevisionCommand(page.Id, T1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static BlockRevision Row(
        Page page,
        Block block,
        long blockVersion,
        BlockChangeKind kind,
        string sortKey,
        string contentJson,
        string plainText,
        DateTimeOffset atUtc
    )
        => BlockRevision.Record(
            page.Id,
            block.Id,
            Guid.CreateVersion7(),
            blockVersion,
            kind,
            RevisionSource.Human,
            null,
            sortKey,
            block.Type,
            contentJson,
            plainText,
            subtreeJson: null,
            atUtc: atUtc
        );

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook) => Page.Create(notebook.Id, null, "Page", "page", "a");

    private static Block NewBlock(Page page, string sortKey, string contentJson, string plainText)
        => Block.Create(page.Id, null, "paragraph", contentJson, plainText, sortKey);

    private static (RestorePageToRevisionCommandHandler Handler, StubBlockRepository Blocks) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page page,
        StubBlockRevisionRepository revisions,
        Block[] seededBlocks
    )
    {
        var blocks = new StubBlockRepository();
        blocks.AddRange(seededBlocks);
        return (
            new RestorePageToRevisionCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                new StubPageRepository { page },
                blocks,
                revisions,
                new StubChangeSourceAccessor(),
                new StubUnitOfWork()
            ),
            blocks
        );
    }
}
