using CodeCafe.Application.Blocks.DeleteBlock;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Blocks;

public sealed class DeleteBlockCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesTheWholeSubtree_AndRepairsTheChain()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        // Top level: predecessor -> root -> successor; root heads child -> grandchild.
        var predecessor = NewBlock(page);
        var root = NewBlock(page);
        var successor = NewBlock(page);
        var child = NewBlock(page, root);
        var grandchild = NewBlock(page, child);
        BlockChain.Link(predecessor, page, parent: null, after: null);
        BlockChain.Link(root, page, parent: null, after: predecessor);
        BlockChain.Link(successor, page, parent: null, after: root);
        BlockChain.Link(child, page, root, after: null);
        BlockChain.Link(grandchild, page, child, after: null);
        var (handler, blocks) = CreateHandler(owner.Id, notebook, [page], [predecessor, root, successor, child, grandchild]);

        var result = await handler.Handle(new DeleteBlockCommand(page.Id, root.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(blocks, block => block.Id == root.Id || block.Id == child.Id || block.Id == grandchild.Id);
        Assert.Equal(successor.Id, predecessor.NextSiblingId); // the predecessor skips the doomed root
        Assert.Equal(2, blocks.Count);
    }

    [Fact]
    public async Task Handle_DeletingTheChainHead_MovesTheHeadPointer()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var head = NewBlock(page);
        var next = NewBlock(page);
        BlockChain.Link(head, page, parent: null, after: null);
        BlockChain.Link(next, page, parent: null, after: head);
        var (handler, _) = CreateHandler(owner.Id, notebook, [page], [head, next]);

        var result = await handler.Handle(new DeleteBlockCommand(page.Id, head.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(next.Id, page.FirstBlockId);
    }

    [Fact]
    public async Task Handle_DeletingANestedHead_ReheadsTheParent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var parent = NewBlock(page);
        var head = NewBlock(page, parent);
        var next = NewBlock(page, parent);
        BlockChain.Link(parent, page, parent: null, after: null);
        BlockChain.Link(head, page, parent, after: null);
        BlockChain.Link(next, page, parent, after: head);
        var (handler, _) = CreateHandler(owner.Id, notebook, [page], [parent, head, next]);

        var result = await handler.Handle(new DeleteBlockCommand(page.Id, head.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(next.Id, parent.FirstChildId);
    }

    [Fact]
    public async Task Handle_BlockOfAnotherPage_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var otherPage = Page.Create(notebook.Id, null, "Other", "other", "b");
        var block = NewBlock(otherPage);
        var (handler, blocks) = CreateHandler(owner.Id, notebook, [page, otherPage], [block]);

        var result = await handler.Handle(new DeleteBlockCommand(page.Id, block.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.NotFound, result.Error);
        Assert.Single(blocks);
    }

    [Fact]
    public async Task Handle_UnknownBlock_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _) = CreateHandler(owner.Id, notebook, [page], []);

        var result = await handler.Handle(new DeleteBlockCommand(page.Id, Guid.NewGuid()), CancellationToken.None);

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
        var block = NewBlock(page);
        var (handler, blocks) = CreateHandler(stranger.Id, notebook, [page], [block]);

        var result = await handler.Handle(new DeleteBlockCommand(page.Id, block.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Single(blocks); // nothing was removed
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook) => Page.Create(notebook.Id, null, "Page", "page", "a");

    private static Block NewBlock(Page page, Block? parent = null)
        => Block.Create(page.Id, parent?.Id, "paragraph", """{"spans":[]}""", string.Empty, "a");

    private static (DeleteBlockCommandHandler Handler, StubBlockRepository Blocks) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page[] seededPages,
        Block[] seededBlocks
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var blocks = new StubBlockRepository();
        blocks.AddRange(seededBlocks);
        return (
            new DeleteBlockCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                blocks,
                new StubBlockRevisionRepository(),
                new StubChangeSourceAccessor(),
                new StubUnitOfWork()
            ),
            blocks
        );
    }
}
