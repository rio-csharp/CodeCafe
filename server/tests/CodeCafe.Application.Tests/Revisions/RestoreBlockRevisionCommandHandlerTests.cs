using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Revisions.RestoreBlockRevision;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests.Revisions;

public sealed class RestoreBlockRevisionCommandHandlerTests
{
    [Fact]
    public async Task Handle_LiveBlock_RestoresTheSnapshotPayload_AndRecordsIt()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var block = NewBlock(page, """{"spans":[{"text":"current","marks":[]}]}""", "current");
        BlockChain.Link(block, page, parent: null, after: null);
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, block, 1, BlockChangeKind.Updated, """{"spans":[{"text":"original","marks":[]}]}""", "original"),
        };
        var (handler, blocks) = CreateHandler(owner.Id, notebook, page, revisions, block);

        var result = await handler.Handle(new RestoreBlockRevisionCommand(page.Id, block.Id, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("original", block.ContentJson);
        Assert.Equal("original", block.PlainText);
        // The restore itself is one more entry in the log, so it can be undone.
        var recorded = Assert.Single(revisions, row => row.ChangeKind == BlockChangeKind.Updated && row.PlainText == "original" && row.BlockVersion == block.Version);
        Assert.Equal(block.Version, recorded.BlockVersion);
        Assert.Single(blocks); // structure untouched
    }

    [Fact]
    public async Task Handle_DeletedBlock_RecreatesItWithItsOriginalId()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var gone = NewBlock(page, """{"spans":[{"text":"lost","marks":[]}]}""", "lost");
        var revisions = new StubBlockRevisionRepository
        {
            Row(page, gone, 3, BlockChangeKind.Updated, gone.ContentJson, gone.PlainText),
        };
        var (handler, blocks) = CreateHandler(owner.Id, notebook, page, revisions);

        var result = await handler.Handle(new RestoreBlockRevisionCommand(page.Id, gone.Id, 3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var recreated = Assert.Single(blocks);
        Assert.Equal(gone.Id, recreated.Id); // original id preserved: references keep working
        Assert.Equal("lost", recreated.PlainText);
        Assert.Null(recreated.ParentBlockId); // the recorded parent is gone too: top level
        Assert.Equal(page.FirstBlockId, recreated.Id); // spliced into the chain
        Assert.Single(revisions, row => row.ChangeKind == BlockChangeKind.Added && row.BlockId == gone.Id);
    }

    [Fact]
    public async Task Handle_DeletedSubtreeRow_BringsBackTheWholeSubtree()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var root = NewBlock(page);
        var child = NewBlock(page, root);
        var grandchild = NewBlock(page, child);
        BlockChain.Link(root, page, parent: null, after: null);
        BlockChain.Link(child, page, root, after: null);
        BlockChain.Link(grandchild, page, child, after: null);
        var revisions = new StubBlockRevisionRepository();
        revisions.AddRange(RevisionRecording.Deleted([root, child, grandchild], Guid.CreateVersion7(), RevisionSource.Human));
        var (handler, blocks) = CreateHandler(owner.Id, notebook, page, revisions);

        var result = await handler.Handle(new RestoreBlockRevisionCommand(page.Id, root.Id, root.Version), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(3, blocks.Count);
        Assert.Equal(new[] { root.Id, child.Id, grandchild.Id }.OrderBy(id => id), blocks.Select(block => block.Id).OrderBy(id => id));
        var recreatedRoot = blocks.Single(block => block.Id == root.Id);
        var recreatedChild = blocks.Single(block => block.Id == child.Id);
        Assert.Equal(child.Id, recreatedRoot.FirstChildId);
        Assert.Equal(grandchild.Id, recreatedChild.FirstChildId);
        Assert.Equal(3, revisions.Count(row => row.ChangeKind == BlockChangeKind.Added));
    }

    [Fact]
    public async Task Handle_SnapshotIdStillAlive_ReturnsRestoreConflict()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var root = NewBlock(page);
        var child = NewBlock(page, root);
        BlockChain.Link(root, page, parent: null, after: null);
        BlockChain.Link(child, page, root, after: null);
        var revisions = new StubBlockRevisionRepository();
        revisions.AddRange(RevisionRecording.Deleted([root, child], Guid.CreateVersion7(), RevisionSource.Human));
        // The child was individually brought back earlier; restoring the root's snapshot now
        // would duplicate it.
        var (handler, _) = CreateHandler(owner.Id, notebook, page, revisions, child);

        var result = await handler.Handle(new RestoreBlockRevisionCommand(page.Id, root.Id, root.Version), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RevisionErrors.RestoreConflict, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownBlockVersion_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _) = CreateHandler(owner.Id, notebook, page, new StubBlockRevisionRepository());

        var result = await handler.Handle(new RestoreBlockRevisionCommand(page.Id, Guid.CreateVersion7(), 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RevisionErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_DeniesNonWriter()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _) = CreateHandler(stranger.Id, notebook, page, new StubBlockRevisionRepository());

        var result = await handler.Handle(new RestoreBlockRevisionCommand(page.Id, Guid.CreateVersion7(), 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static BlockRevision Row(Page page, Block block, long blockVersion, BlockChangeKind kind, string contentJson, string plainText)
        => BlockRevision.Record(
            page.Id,
            block.Id,
            Guid.CreateVersion7(),
            blockVersion,
            kind,
            RevisionSource.Human,
            block.ParentBlockId,
            block.SortKey,
            block.Type,
            contentJson,
            plainText
        );

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook) => Page.Create(notebook.Id, null, "Page", "page", "a");

    private static Block NewBlock(Page page, string contentJson, string plainText)
        => Block.Create(page.Id, null, "paragraph", contentJson, plainText, "a");

    private static Block NewBlock(Page page, Block? parent = null)
        => Block.Create(page.Id, parent?.Id, "paragraph", """{"spans":[]}""", string.Empty, "a");

    private static (RestoreBlockRevisionCommandHandler Handler, StubBlockRepository Blocks) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page page,
        StubBlockRevisionRepository revisions,
        params Block[] seededBlocks
    )
    {
        var blocks = new StubBlockRepository();
        blocks.AddRange(seededBlocks);
        return (
            new RestoreBlockRevisionCommandHandler(
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
