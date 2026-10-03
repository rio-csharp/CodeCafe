using System.Text.Json;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.UpdateBlock;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Blocks;

public sealed class UpdateBlockCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesContent_BumpsVersion_AndRegeneratesPlainText()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var block = NewBlock(page, """{"spans":[{"text":"Before","marks":[]}]}""", "Before");
        var (handler, _, _) = CreateHandler(owner.Id, notebook, [page], [block]);

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, block.Id, Paragraph("After"), BaseVersion: 1),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, block.Version);
        Assert.Equal("After", block.PlainText);
        Assert.Equal(2, result.Value!.Version);
        Assert.Equal("""{"spans":[{"text":"After","marks":[]}]}""", result.Value.Content.GetRawText());
    }

    [Fact]
    public async Task Handle_StaleBaseVersion_ReturnsVersionConflict()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var block = NewBlock(page, """{"spans":[]}""", string.Empty);
        block.UpdateContent("""{"spans":[{"text":"Newer","marks":[]}]}""", "Newer"); // Version now 2
        var (handler, _, _) = CreateHandler(owner.Id, notebook, [page], [block]);

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, block.Id, Paragraph("Stale write"), BaseVersion: 1),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.VersionConflict, result.Error);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictFromTheSave_ReturnsVersionConflict()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var block = NewBlock(page, """{"spans":[]}""", string.Empty);
        var (handler, _, _) = CreateHandler(
            owner.Id,
            notebook,
            [page],
            [block],
            new StubUnitOfWork(new ConcurrencyConflictException())
        );

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, block.Id, Paragraph("Lost race"), BaseVersion: 1),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.VersionConflict, result.Error);
    }

    [Fact]
    public async Task Handle_BlockOfAnotherPage_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var otherPage = Page.Create(notebook.Id, null, "Other", "other", "b");
        var block = NewBlock(otherPage, """{"spans":[]}""", string.Empty);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, [page, otherPage], [block]);

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, block.Id, Paragraph("Nope"), BaseVersion: 1),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownBlock_ReturnsNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, [page], []);

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, Guid.NewGuid(), Paragraph("Nope"), BaseVersion: 1),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_InvalidPayload_ReturnsValidationError()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var block = NewBlock(page, """{"spans":[]}""", string.Empty);
        var (handler, _, _) = CreateHandler(owner.Id, notebook, [page], [block]);

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, block.Id, Json("""{"spans":[],"surprise":true}"""), BaseVersion: 1),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockErrors.InvalidBlockPayload, result.Error);
    }

    [Fact]
    public async Task Handle_DeniesNonWriter()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = SeedPage(notebook);
        var block = NewBlock(page, """{"spans":[]}""", string.Empty);
        var (handler, _, _) = CreateHandler(stranger.Id, notebook, [page], [block]);

        var result = await handler.Handle(
            new UpdateBlockCommand(page.Id, block.Id, Paragraph("Nope"), BaseVersion: 1),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook) => Page.Create(notebook.Id, null, "Page", "page", "a");

    private static Block NewBlock(Page page, string contentJson, string plainText)
        => Block.Create(page.Id, null, "paragraph", contentJson, plainText, "a");

    private static JsonElement Json(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static JsonElement Paragraph(string text)
        => Json($$"""{"spans":[{"text":"{{text}}","marks":[]}]}""");

    private static (UpdateBlockCommandHandler Handler, StubBlockRepository Blocks, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page[] seededPages,
        Block[] seededBlocks,
        StubUnitOfWork? unitOfWork = null
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var blocks = new StubBlockRepository();
        blocks.AddRange(seededBlocks);
        unitOfWork ??= new StubUnitOfWork();
        var revisions = new StubBlockRevisionRepository();
        return (
            new UpdateBlockCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
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
