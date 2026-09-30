using CodeCafe.Application.Blocks.MoveBlock;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Blocks;

public sealed class MoveBlockCommandHandlerTests
{
    [Fact]
    public async Task Handle_NullAfterBlockId_MovesToTheTopLevelHead()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        var b = NewBlock(page, "b");
        var c = NewBlock(page, "c");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(c, page, parent: null, after: b);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page, a, b, c);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, c.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(c.Id, page.FirstBlockId);
        Assert.Equal(a.Id, c.NextSiblingId);
        Assert.Null(b.NextSiblingId);
        Assert.True(string.CompareOrdinal(c.SortKey, a.SortKey) < 0);
        Assert.Equal(2, c.Revision); // only the moved block bumps
        Assert.Equal(1, a.Revision);
    }

    [Fact]
    public async Task Handle_MovesAfterAnotherBlock()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        var b = NewBlock(page, "b");
        var c = NewBlock(page, "c");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(c, page, parent: null, after: b);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page, a, b, c);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, a.Id, b.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(b.Id, page.FirstBlockId);
        Assert.Equal(a.Id, b.NextSiblingId);
        Assert.Equal(c.Id, a.NextSiblingId);
        Assert.True(string.CompareOrdinal(b.SortKey, a.SortKey) < 0);
        Assert.True(string.CompareOrdinal(a.SortKey, c.SortKey) < 0);
    }

    [Fact]
    public async Task Handle_AfterANestedBlock_JoinsItsParentGroup()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var parent = NewBlock(page, "p");
        var c1 = NewBlock(page, "a", parent);
        var c2 = NewBlock(page, "b", parent);
        var x = NewBlock(page, "x");
        BlockChain.Link(parent, page, parent: null, after: null);
        BlockChain.Link(x, page, parent: null, after: parent);
        BlockChain.Link(c1, page, parent, after: null);
        BlockChain.Link(c2, page, parent, after: c1);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page, parent, c1, c2, x);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, x.Id, c1.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(parent.Id, x.ParentBlockId);
        Assert.Equal(x.Id, c1.NextSiblingId);
        Assert.Equal(c2.Id, x.NextSiblingId);
        // The old top-level chain was repaired: parent no longer points at x.
        Assert.Null(parent.NextSiblingId);
        Assert.Equal(parent.Id, page.FirstBlockId);
        Assert.True(string.CompareOrdinal(c1.SortKey, x.SortKey) < 0);
        Assert.True(string.CompareOrdinal(x.SortKey, c2.SortKey) < 0);
    }

    [Fact]
    public async Task Handle_MoveUnderOwnDescendant_ReturnsInvalidBlockPosition_NotAnException()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var parent = NewBlock(page, "p");
        var child = NewBlock(page, "a", parent);
        var grandchild = NewBlock(page, "a", child);
        BlockChain.Link(parent, page, parent: null, after: null);
        BlockChain.Link(child, page, parent, after: null);
        BlockChain.Link(grandchild, page, child, after: null);
        var (handler, _, unitOfWork) = CreateHandler(owner.Id, notebook, page, parent, child, grandchild);

        // Moving parent after grandchild would land it under child — its own descendant.
        var result = await handler.Handle(new MoveBlockCommand(page.Id, parent.Id, grandchild.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPosition, result.Error);
        Assert.Equal(parent.Id, page.FirstBlockId); // nothing moved
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_AfterItself_ReturnsInvalidBlockPosition()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        BlockChain.Link(a, page, parent: null, after: null);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page, a);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, a.Id, a.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPosition, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownAfterBlock_ReturnsInvalidBlockPosition()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        BlockChain.Link(a, page, parent: null, after: null);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page, a);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, a.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPosition, result.Error);
    }

    [Fact]
    public async Task Handle_BlockOfAnotherPage_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var otherPage = Page.Create(notebook.Id, null, "Other", "other", "b");
        var block = NewBlock(otherPage, "a");
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page, block);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, block.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownBlock_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_DeniesNonWriter()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var a = NewBlock(page, "a");
        var (handler, _, unitOfWork) = CreateHandler(stranger.Id, notebook, page, a);

        var result = await handler.Handle(new MoveBlockCommand(page.Id, a.Id, null), CancellationToken.None);

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

    private static (MoveBlockCommandHandler Handler, StubBlockRepository Blocks, StubUnitOfWork UnitOfWork) CreateHandler(
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
            new MoveBlockCommandHandler(
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
