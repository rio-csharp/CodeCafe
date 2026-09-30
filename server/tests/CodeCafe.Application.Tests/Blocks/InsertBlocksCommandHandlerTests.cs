using System.Text.Json;
using CodeCafe.Application.Blocks.InsertBlocks;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Blocks;

public sealed class InsertBlocksCommandHandlerTests
{
    [Fact]
    public async Task Handle_NullAfterBlockId_InsertsAtTheTopLevelHead()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var old = NewBlock(page, "m");
        BlockChain.Link(old, page, parent: null, after: null);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page, old);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, null, [Input(Paragraph("Hello"))]),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var created = Assert.Single(result.Value!);
        Assert.Equal(created.Id, page.FirstBlockId);
        var inserted = blocks.Single(block => block.Id == created.Id);
        Assert.Equal(old.Id, inserted.NextSiblingId);
        Assert.Null(inserted.ParentBlockId);
        Assert.True(string.CompareOrdinal(inserted.SortKey, old.SortKey) < 0);
    }

    [Fact]
    public async Task Handle_InsertsAfterTheGivenBlock()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        var b = NewBlock(page, "b");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page, a, b);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, a.Id, [Input(Paragraph("Between"))]),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var inserted = blocks.Single(block => block.Id == result.Value![0].Id);
        Assert.Equal(inserted.Id, a.NextSiblingId);
        Assert.Equal(b.Id, inserted.NextSiblingId);
        Assert.True(string.CompareOrdinal(a.SortKey, inserted.SortKey) < 0);
        Assert.True(string.CompareOrdinal(inserted.SortKey, b.SortKey) < 0);
    }

    [Fact]
    public async Task Handle_MultipleBlocks_StayConsecutiveInInputOrder()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var old = NewBlock(page, "m");
        BlockChain.Link(old, page, parent: null, after: null);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page, old);

        var result = await handler.Handle(
            new InsertBlocksCommand(
                page.Id,
                null,
                [Input(Paragraph("One")), Input(Paragraph("Two")), Input(Paragraph("Three"))]
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Count);
        var first = blocks.Single(block => block.Id == result.Value[0].Id);
        var second = blocks.Single(block => block.Id == result.Value[1].Id);
        var third = blocks.Single(block => block.Id == result.Value[2].Id);
        Assert.Equal(first.Id, page.FirstBlockId);
        Assert.Equal(second.Id, first.NextSiblingId);
        Assert.Equal(third.Id, second.NextSiblingId);
        Assert.Equal(old.Id, third.NextSiblingId);
        Assert.True(string.CompareOrdinal(first.SortKey, second.SortKey) < 0);
        Assert.True(string.CompareOrdinal(second.SortKey, third.SortKey) < 0);
        Assert.True(string.CompareOrdinal(third.SortKey, old.SortKey) < 0);
    }

    [Fact]
    public async Task Handle_InsertAfterAChildBlock_JoinsTheSameParentGroup()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var parent = NewBlock(page, "p");
        var c1 = NewBlock(page, "a", parent);
        var c2 = NewBlock(page, "b", parent);
        BlockChain.Link(parent, page, parent: null, after: null);
        BlockChain.Link(c1, page, parent, after: null);
        BlockChain.Link(c2, page, parent, after: c1);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page, parent, c1, c2);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, c1.Id, [Input(Paragraph("Nested"))]),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var inserted = blocks.Single(block => block.Id == result.Value![0].Id);
        Assert.Equal(parent.Id, inserted.ParentBlockId);
        Assert.Equal(inserted.Id, c1.NextSiblingId);
        Assert.Equal(c2.Id, inserted.NextSiblingId);
    }

    [Fact]
    public async Task Handle_InvalidPayload_FailsAndAddsNothing()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, blocks, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, null, [Input(Json("""{"spans":[],"surprise":true}"""))]),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload, result.Error);
        Assert.Empty(blocks);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_UnknownType_IsRejected()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, null, [Input(Json("{}"), type: "table")]),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.UnsupportedBlockType, result.Error);
        Assert.Empty(blocks);
    }

    [Fact]
    public async Task Handle_UnknownAfterBlock_ReturnsInvalidBlockPosition()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, blocks, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, Guid.NewGuid(), [Input(Paragraph("Lost"))]),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPosition, result.Error);
        Assert.Empty(blocks);
    }

    [Fact]
    public async Task Handle_DeniesAnonymous()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, _) = CreateHandler(null, notebook, page);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, null, [Input(Paragraph("Nope"))]),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_DeniesNonWriter()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, _) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(
            new InsertBlocksCommand(page.Id, null, [Input(Paragraph("Nope"))]),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
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

    private static BlockInput Input(JsonElement content, string type = BlockTypes.Paragraph) => new(null, type, content);

    private static (InsertBlocksCommandHandler Handler, StubBlockRepository Blocks, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid? currentUserId,
        Notebook notebook,
        Page page,
        params Block[] seededBlocks
    )
    {
        var blocks = new StubBlockRepository();
        blocks.AddRange(seededBlocks);
        var unitOfWork = new StubUnitOfWork();
        return (
            new InsertBlocksCommandHandler(
                new StubCurrentUserAccessor(currentUserId is { } id ? new CurrentUser(id) : null),
                new StubNotebookRepository { notebook },
                new StubPageRepository { page },
                blocks,
                unitOfWork
            ),
            blocks,
            unitOfWork
        );
    }
}
