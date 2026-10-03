using System.Text.Json;

using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Blocks.DeleteBlock;
using CodeCafe.Application.Blocks.InsertBlocks;
using CodeCafe.Application.Blocks.MoveBlock;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.UpdateBlock;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Tests.Revisions;

// The five block write handlers (plus the import) must each feed the revision log; these tests
// pin that wiring so a handler losing its recording call fails loudly here.
public sealed class BlockWriteRecordingTests
{
    [Fact]
    public async Task Insert_RecordsOneAddedRowPerBlock_SharingOneBatch()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var blocks = new StubBlockRepository();
        var revisions = new StubBlockRevisionRepository();
        var handler = new InsertBlocksCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            blocks,
            revisions,
            new StubChangeSourceAccessor(),
            new StubUnitOfWork()
        );

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, null, [Input("one"), Input("two")]),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, revisions.Count);
        Assert.All(revisions, row => Assert.Equal(BlockChangeKind.Added, row.ChangeKind));
        Assert.All(revisions, row => Assert.Equal(RevisionSource.Human, row.Source));
        Assert.Single(revisions.Select(row => row.BatchId).Distinct());
        Assert.All(revisions, row => Assert.Equal(page.Id, row.PageId));
        Assert.Equal(blocks.Select(block => block.Id).OrderBy(id => id), revisions.Select(row => row.BlockId).OrderBy(id => id));
    }

    [Fact]
    public async Task Update_RecordsAnUpdatedRow_WithTheNormalizedSnapshot()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var block = NewBlock(page);
        BlockChain.Link(block, page, parent: null, after: null);
        var revisions = new StubBlockRevisionRepository();
        var handler = new UpdateBlockCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository { block },
            revisions,
            new StubChangeSourceAccessor(),
            new StubUnitOfWork()
        );

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, block.Id, Paragraph("changed"), block.Version),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var row = Assert.Single(revisions);
        Assert.Equal(BlockChangeKind.Updated, row.ChangeKind);
        Assert.Equal(block.Id, row.BlockId);
        Assert.Equal(block.Version, row.BlockVersion);
        Assert.Equal(block.ContentJson, row.ContentJson);
        Assert.Equal("changed", row.PlainText);
    }

    [Fact]
    public async Task Move_RecordsAMovedRow_WithTheNewPlacement()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var first = NewBlock(page);
        var second = NewBlock(page);
        BlockChain.Link(first, page, parent: null, after: null);
        BlockChain.Link(second, page, parent: null, after: first);
        var revisions = new StubBlockRevisionRepository();
        var handler = new MoveBlockCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository { first, second },
            revisions,
            new StubChangeSourceAccessor(),
            new StubUnitOfWork()
        );

        // Move `first` to the tail: after `second`.
        var result = await handler.Handle(new MoveBlockCommand(page.Id, first.Id, second.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(revisions);
        Assert.Equal(BlockChangeKind.Moved, row.ChangeKind);
        Assert.Equal(first.Id, row.BlockId);
        Assert.Equal(first.SortKey, row.SortKey);
    }

    [Fact]
    public async Task Delete_RecordsEveryNode_WithTheSubtreeSnapshotOnTheRootOnly()
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
        var handler = new DeleteBlockCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository { root, child, grandchild },
            revisions,
            new StubChangeSourceAccessor(),
            new StubUnitOfWork()
        );

        var result = await handler.Handle(new DeleteBlockCommand(page.Id, root.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, revisions.Count);
        Assert.All(revisions, row => Assert.Equal(BlockChangeKind.Deleted, row.ChangeKind));

        var rootRow = Assert.Single(revisions, row => row.BlockId == root.Id);
        Assert.NotNull(rootRow.SubtreeJson);
        using var snapshot = JsonDocument.Parse(rootRow.SubtreeJson!);
        Assert.Equal(root.Id, snapshot.RootElement.GetProperty("id").GetGuid());
        var childNode = Assert.Single(snapshot.RootElement.GetProperty("children").EnumerateArray());
        Assert.Equal(child.Id, childNode.GetProperty("id").GetGuid());
        var grandchildNode = Assert.Single(childNode.GetProperty("children").EnumerateArray());
        Assert.Equal(grandchild.Id, grandchildNode.GetProperty("id").GetGuid());

        // Non-root nodes carry no snapshot: they are reachable from the root's.
        Assert.All(
            revisions.Where(row => row.BlockId != root.Id),
            row => Assert.Null(row.SubtreeJson)
        );
    }

    [Fact]
    public async Task ApplyBlockOps_DryRun_RecordsNothing()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var revisions = new StubBlockRevisionRepository();
        var handler = new ApplyBlockOpsCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository(),
            revisions,
            new StubChangeSourceAccessor(),
            new StubUnitOfWork()
        );

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: null, Type: BlockTypes.Paragraph, After: null, Content: Paragraph("draft"), BaseVersion: null)],
                DryRun: true
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(revisions);
    }

    [Fact]
    public async Task ApplyBlockOps_RecordsTheWholeBatchUnderOneBatchId()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var existing = NewBlock(page);
        BlockChain.Link(existing, page, parent: null, after: null);
        var revisions = new StubBlockRevisionRepository();
        var handler = new ApplyBlockOpsCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository { existing },
            revisions,
            new StubChangeSourceAccessor(),
            new StubUnitOfWork()
        );

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [
                    new BlockOp(BlockOpKind.Update, BlockId: existing.Id.ToString(), TempId: null, Type: null, After: null, Content: Paragraph("edited"), BaseVersion: existing.Version),
                    new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: "temp-1", Type: BlockTypes.Paragraph, After: null, Content: Paragraph("fresh"), BaseVersion: null),
                    new BlockOp(BlockOpKind.Delete, BlockId: "temp-1", TempId: null, Type: null, After: null, Content: null, BaseVersion: null),
                ],
                DryRun: false
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(3, revisions.Count);
        Assert.Single(revisions.Select(row => row.BatchId).Distinct());
        Assert.Equal(
            [BlockChangeKind.Updated, BlockChangeKind.Added, BlockChangeKind.Deleted],
            revisions.Select(row => row.ChangeKind).ToArray()
        );
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook) => Page.Create(notebook.Id, null, "Page", "page", "a");

    private static Block NewBlock(Page page, Block? parent = null)
        => Block.Create(page.Id, parent?.Id, BlockTypes.Paragraph, """{"spans":[]}""", string.Empty, "a");

    private static BlockInput Input(string text) => new(null, BlockTypes.Paragraph, Paragraph(text));

    private static JsonElement Paragraph(string text)
        => JsonSerializer.Deserialize<JsonElement>($$"""{"spans":[{"text":"{{text}}","marks":[]}]}""");
}
