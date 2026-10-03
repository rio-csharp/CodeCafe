using System.Text.Json;

using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class ExportPageQueryHandlerTests
{
    [Fact]
    public async Task Handle_OwnerExportsPage_WithSlugFileNameAndBlocks()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "My Page", "my-page", "a");
        var blocks = new StubBlockRepository();
        AddBlock(blocks, page, """{"spans":[{"text":"hello","marks":[]}]}""");
        var handler = CreateHandler(owner.Id, notebook, page, blocks);

        var result = await handler.Handle(new ExportPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("my-page.md", result.Value!.FileName);
        var markdown = result.Value.Markdown.ReplaceLineEndings("\n");
        Assert.StartsWith("# My Page\n", markdown);
        Assert.Contains("\nhello\n", markdown);
    }

    [Fact]
    public async Task Handle_StrangerGetsNotFound()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner); // private
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var handler = CreateHandler(stranger.Id, notebook, page, new StubBlockRepository());

        var result = await handler.Handle(new ExportPageQuery(page.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_NoCurrentUser_GetsNotFound()
    {
        var notebook = SeedNotebook(SeedOwner());
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var handler = new ExportPageQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository { notebook },
            new StubPageRepository { page },
            new StubBlockRepository(),
            new StubPasswordHasher()
        );

        var result = await handler.Handle(new ExportPageQuery(page.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownPage_GetsNotFound()
    {
        var owner = SeedOwner();
        var handler = CreateHandler(owner.Id, SeedNotebook(owner), null, new StubBlockRepository());

        var result = await handler.Handle(new ExportPageQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_PageShareGrantsExport_WithoutNotebookAccess()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner); // private
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(guest.Id, CollaboratorRole.Viewer);
        var handler = CreateHandler(guest.Id, notebook, page, new StubBlockRepository());

        var result = await handler.Handle(new ExportPageQuery(page.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AccessCodeNotebook_RequiresAndAcceptsTheCode()
    {
        var owner = SeedOwner();
        var guest = User.Create("guest@example.com", "guest@example.com", "Guest", "hash");
        var notebook = SeedNotebook(owner, NotebookVisibility.Unlisted);
        notebook.SetAccessCodeHash("hashed:pw"); // matches StubPasswordHasher's Verify
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var handler = CreateHandler(guest.Id, notebook, page, new StubBlockRepository());

        var withoutCode = await handler.Handle(new ExportPageQuery(page.Id), CancellationToken.None);
        var withCode = await handler.Handle(new ExportPageQuery(page.Id, "pw"), CancellationToken.None);

        Assert.False(withoutCode.IsSuccess);
        Assert.Equal(NotebookErrors.AccessCodeRequired, withoutCode.Error);
        Assert.True(withCode.IsSuccess);
    }

    private static ExportPageQueryHandler CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        Page? page,
        StubBlockRepository blocks
    )
    {
        var pages = new StubPageRepository();
        if (page is not null)
        {
            pages.Add(page);
        }

        return new ExportPageQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
            new StubNotebookRepository { notebook },
            pages,
            blocks,
            new StubPasswordHasher()
        );
    }

    private static void AddBlock(StubBlockRepository blocks, Page page, string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var normalized = BlockPayloads.ValidateAndNormalize(BlockTypes.Paragraph, document.RootElement);
        Assert.True(normalized.IsSuccess, normalized.Error?.Message);
        var block = Block.Create(
            page.Id,
            null,
            BlockTypes.Paragraph,
            normalized.Value!.CanonicalJson,
            normalized.Value.PlainText,
            BlockSiblingSortKeys.KeyForInsert([], 0)
        );
        BlockChain.Insert(block, page, null, null, null);
        blocks.Add(block);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, NotebookVisibility visibility = NotebookVisibility.Private)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", visibility);
}
