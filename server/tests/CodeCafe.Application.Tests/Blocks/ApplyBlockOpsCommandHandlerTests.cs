using System.Text.Json;
using CodeCafe.Application.Blocks.ApplyBlockOps;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Blocks;

public sealed class ApplyBlockOpsCommandHandlerTests
{
    [Fact]
    public async Task Handle_InsertWithTempId_ThenMoveAfterTempId_WorksInOneBatch()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        BlockChain.Link(a, page, parent: null, after: null);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page, a);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [
                    new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: "t1", Type: BlockTypes.Paragraph, After: null, Content: Paragraph("Hello"), BaseVersion: null),
                    new BlockOp(BlockOpKind.Move, BlockId: a.Id.ToString(), TempId: null, Type: null, After: "t1", Content: null, BaseVersion: null),
                ]
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        var inserted = result.Value[0];
        Assert.Equal("t1", inserted.TempId);
        Assert.NotNull(inserted.BlockId);
        Assert.Equal(1, inserted.Version);
        Assert.NotNull(inserted.Content);

        var moved = result.Value[1];
        Assert.Null(moved.TempId);
        Assert.Equal(a.Id, moved.BlockId);
        Assert.Equal(2, moved.Version); // the move bumped the pre-existing block

        var insertedBlock = blocks.Single(block => block.Id == inserted.BlockId);
        Assert.Equal(insertedBlock.Id, page.FirstBlockId);
        Assert.Equal(a.Id, insertedBlock.NextSiblingId);
        Assert.Null(a.NextSiblingId);
        Assert.Equal(2, blocks.Count);
    }

    [Fact]
    public async Task Handle_UpdatesAgainstABatchCreatedBlock_NeedProgressiveBaseVersions()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [
                    new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: "t1", Type: BlockTypes.Paragraph, After: null, Content: Paragraph("v1"), BaseVersion: null),
                    new BlockOp(BlockOpKind.Update, BlockId: "t1", TempId: null, Type: null, After: null, Content: Paragraph("v2"), BaseVersion: 1),
                    new BlockOp(BlockOpKind.Update, BlockId: "t1", TempId: null, Type: null, After: null, Content: Paragraph("v3"), BaseVersion: 2),
                ]
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal([1L, 2L, 3L], result.Value!.Select(op => op.Version).ToList());
        var block = blocks.Single();
        Assert.Equal(3, block.Version);
        Assert.Equal("v3", block.PlainText);
    }

    [Fact]
    public async Task Handle_StaleBaseVersionAgainstABatchCreatedBlock_FailsTheWholeBatch()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, blocks, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [
                    new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: "t1", Type: BlockTypes.Paragraph, After: null, Content: Paragraph("v1"), BaseVersion: null),
                    new BlockOp(BlockOpKind.Update, BlockId: "t1", TempId: null, Type: null, After: null, Content: Paragraph("v2"), BaseVersion: 1),
                    // BaseVersion 1 was valid at batch start but the previous op bumped it.
                    new BlockOp(BlockOpKind.Update, BlockId: "t1", TempId: null, Type: null, After: null, Content: Paragraph("v3"), BaseVersion: 1),
                ]
            ),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.VersionConflict, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReferencingABlockDeletedEarlierInTheBatch_FailsTheWholeBatch()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        var b = NewBlock(page, "b");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, page, a, b);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [
                    new BlockOp(BlockOpKind.Delete, BlockId: a.Id.ToString(), TempId: null, Type: null, After: null, Content: null, BaseVersion: null),
                    new BlockOp(BlockOpKind.Update, BlockId: a.Id.ToString(), TempId: null, Type: null, After: null, Content: Paragraph("zombie"), BaseVersion: 1),
                ]
            ),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_DryRun_MutatesNothing_ButReturnsCanonicalContent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        BlockChain.Link(a, page, parent: null, after: null);
        var (handler, blocks, unitOfWork) = CreateHandler(owner.Id, notebook, page, a);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [
                    // Code normalization lowercases and trims the language: canonical proof.
                    new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: "t1", Type: BlockTypes.Code, After: null, Content: Json("""{"code":"x=1","language":"CSharp"}"""), BaseVersion: null),
                    new BlockOp(BlockOpKind.Update, BlockId: "t1", TempId: null, Type: null, After: null, Content: Json("""{"code":"y=2","language":" FSharp "}"""), BaseVersion: 1),
                ],
                DryRun: true
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("csharp", result.Value[0].Content!.Value.GetProperty("language").GetString());
        Assert.Equal("fsharp", result.Value[1].Content!.Value.GetProperty("language").GetString());
        Assert.Equal(2, result.Value[1].Version);

        // Nothing persisted: the seeded block is the repository's only content, untouched.
        Assert.Single(blocks);
        Assert.Equal(1, a.Version);
        Assert.Null(a.NextSiblingId);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_UnknownTempIdBlockReference_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [new BlockOp(BlockOpKind.Update, BlockId: "ghost", TempId: null, Type: null, After: null, Content: Paragraph("boo"), BaseVersion: 1)]
            ),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_UnknownTempIdAfterReference_ReturnsInvalidBlockPosition()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: null, Type: BlockTypes.Paragraph, After: "ghost", Content: Paragraph("boo"), BaseVersion: null)]
            ),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPosition, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_DuplicateTempId_FailsTheWholeBatch()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [
                    new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: "t1", Type: BlockTypes.Paragraph, After: null, Content: Paragraph("one"), BaseVersion: null),
                    new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: "t1", Type: BlockTypes.Paragraph, After: null, Content: Paragraph("two"), BaseVersion: null),
                ]
            ),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.DuplicateTempId, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_MoveUnderOwnDescendant_ReturnsInvalidBlockPosition_NotAnException()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var parent = NewBlock(page, "p");
        var child = NewBlock(page, "a", parent);
        BlockChain.Link(parent, page, parent: null, after: null);
        BlockChain.Link(child, page, parent, after: null);
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, page, parent, child);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [new BlockOp(BlockOpKind.Move, BlockId: parent.Id.ToString(), TempId: null, Type: null, After: child.Id.ToString(), Content: null, BaseVersion: null)]
            ),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPosition, result.Error);
        Assert.Equal(parent.Id, page.FirstBlockId); // nothing moved
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_DeniesNonWriter()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, unitOfWork) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(
            new ApplyBlockOpsCommand(
                page.Id,
                [new BlockOp(BlockOpKind.Insert, BlockId: null, TempId: null, Type: BlockTypes.Paragraph, After: null, Content: Paragraph("nope"), BaseVersion: null)]
            ),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook) => Page.Create(notebook.Id, null, "Page", "page", "a");

    private static Block NewBlock(Page page, string sortKey, Block? parent = null)
        => Block.Create(page.Id, parent?.Id, "paragraph", """{"spans":[]}""", string.Empty, sortKey);

    private static JsonElement Json(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static JsonElement Paragraph(string text)
        => Json($$"""{"spans":[{"text":"{{text}}","marks":[]}]}""");

    private static (ApplyBlockOpsCommandHandler Handler, StubBlockRepository Blocks, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid? currentUserId,
        Notebook notebook,
        Page page,
        params Block[] seededBlocks
    )
    {
        var blocks = new StubBlockRepository();
        blocks.AddRange(seededBlocks);
        var unitOfWork = new StubUnitOfWork();
        var revisions = new StubBlockRevisionRepository();
        return (
            new ApplyBlockOpsCommandHandler(
                new StubCurrentUserAccessor(currentUserId is { } id ? new CurrentUser(id) : null),
                new StubNotebookRepository { notebook },
                new StubPageRepository { page },
                blocks,
                revisions,
                new StubChangeSourceAccessor(),
                unitOfWork
            ),
            blocks,
            unitOfWork
        );
    }
}
