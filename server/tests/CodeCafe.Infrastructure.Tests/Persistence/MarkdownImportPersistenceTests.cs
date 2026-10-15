using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Application.Notebooks.ImportNotebook;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Application.Pages.ImportPage;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Tests.Persistence;

// The two import handlers are the only place that inserts a page and its blocks in one request.
// The model holds two pairs of two-way foreign keys — Page.FirstBlockId against Block.PageId, and
// Block.FirstChildId against Block.ParentBlockId — so EF cannot order those inserts inside a
// single SaveChanges and throws a circular-dependency error instead. That failure is invisible to
// the Application-layer tests, whose doubles have no constraint graph, which is why these tests
// run against a real database.
[Collection(nameof(PostgresCollection))]
public sealed class MarkdownImportPersistenceTests(PostgresFixture fixture)
{
    // The first "#" names the page, so the parsed tree is: paragraph, two bulleted items (the
    // first carrying a nested child), code — five blocks, four of them top level.
    private const string PageMarkdown = """
        # Imported page

        A paragraph.

        - first item
          - nested item
        - second item

        ```csharp
        var answer = 42;
        ```
        """;

    private const string NotebookMarkdown = """
        # Imported notebook

        ## First page

        Intro paragraph.

        - top item
          - nested item

        ## Second page

        Closing paragraph.
        """;

    [Fact]
    public async Task PageImport_PersistsTheWholeBlockTree()
    {
        var ct = TestContext.Current.CancellationToken;
        var (context, ownerId, slug) = await SeedNotebookAsync();
        await using var scope = context;

        var result = await NewPageImporter(context, ownerId)
            .Handle(new ImportPageCommand(slug, new PageExportDto("001-async-await.md", PageMarkdown)), ct);

        Assert.True(result.IsSuccess, result.Error?.Message);

        await using var verify = await fixture.CreateContextAsync();
        var page = await verify.Pages.SingleAsync(candidate => candidate.Id == result.Value!.Id, ct);
        var blocks = await verify.Blocks.Where(block => block.PageId == page.Id).ToListAsync(ct);
        Assert.Equal(5, blocks.Count);

        // The chain head is written by a later save than the page row itself — the link EF refused
        // to order — so it is the first thing to check.
        var topLevel = BlockChain.OrderByChain(page, blocks, parentId: null);
        Assert.NotNull(page.FirstBlockId);
        Assert.Equal(
            ["paragraph", "bulleted-list", "bulleted-list", "code"],
            topLevel.Select(block => block.Type)
        );

        // The nested item hangs off the first bullet: FirstChildId and ParentBlockId are the
        // second two-way pair, and both sides have to agree.
        var child = Assert.Single(BlockChain.OrderByChain(page, blocks, topLevel[1].Id));
        Assert.Equal("nested item", child.PlainText);
        Assert.Equal(topLevel[1].Id, child.ParentBlockId);
        Assert.Equal(child.Id, topLevel[1].FirstChildId);

        // Every block lands in the page history, as one batch.
        var revisions = await verify
            .BlockRevisions.Where(revision => revision.PageId == page.Id)
            .ToListAsync(ct);
        Assert.Equal(blocks.Count, revisions.Count);
        Assert.Single(revisions.Select(revision => revision.BatchId).Distinct());
    }

    [Fact]
    public async Task NotebookImport_PersistsEveryPageAndBlock()
    {
        var ct = TestContext.Current.CancellationToken;
        var (context, ownerId, _) = await SeedNotebookAsync();
        await using var scope = context;

        var result = await NewNotebookImporter(context, ownerId)
            .Handle(new ImportNotebookCommand(new NotebookExportDto("notes.md", NotebookMarkdown)), ct);

        Assert.True(result.IsSuccess, result.Error?.Message);

        await using var verify = await fixture.CreateContextAsync();
        var notebook = await verify.Notebooks.SingleAsync(candidate => candidate.Id == result.Value!.Id, ct);
        var pages = await verify
            .Pages.Where(candidate => candidate.NotebookId == notebook.Id)
            .OrderBy(candidate => candidate.SortKey)
            .ToListAsync(ct);

        Assert.Equal(2, pages.Count);
        Assert.Equal(pages.Count, result.Value!.PageCount);

        var pageIds = pages.Select(page => page.Id).ToList();
        var blocks = await verify.Blocks.Where(block => pageIds.Contains(block.PageId)).ToListAsync(ct);
        Assert.Equal(4, blocks.Count);

        var firstPageBlocks = blocks.Where(block => block.PageId == pages[0].Id).ToList();
        var firstPageChain = BlockChain.OrderByChain(pages[0], firstPageBlocks, parentId: null);
        Assert.Equal(["paragraph", "bulleted-list"], firstPageChain.Select(block => block.Type));
        var child = Assert.Single(BlockChain.OrderByChain(pages[0], firstPageBlocks, firstPageChain[1].Id));
        Assert.Equal("nested item", child.PlainText);

        var secondPageChain = BlockChain.OrderByChain(
            pages[1],
            blocks.Where(block => block.PageId == pages[1].Id).ToList(),
            parentId: null
        );
        Assert.Equal(["paragraph"], secondPageChain.Select(block => block.Type));

        Assert.Equal(blocks.Count, await verify.BlockRevisions.CountAsync(revision => pageIds.Contains(revision.PageId), ct));
    }

    private async Task<(AppDbContext Context, Guid OwnerId, string Slug)> SeedNotebookAsync()
    {
        var context = await fixture.CreateCleanContextAsync();
        var owner = User.Create("importer@example.com", "importer@example.com", "Importer", "hash");
        var notebook = Notebook.Create(owner.Id, "Notebook", null, "import-target", NotebookVisibility.Private);
        context.Users.Add(owner);
        context.Notebooks.Add(notebook);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (context, owner.Id, notebook.Slug);
    }

    private static ImportPageCommandHandler NewPageImporter(AppDbContext context, Guid ownerId) =>
        new(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            new PageRepository(context),
            new BlockRepository(context),
            new BlockRevisionRepository(context),
            new TestChangeSourceAccessor(),
            context
        );

    private static ImportNotebookCommandHandler NewNotebookImporter(AppDbContext context, Guid ownerId) =>
        new(
            new TestCurrentUserAccessor(ownerId),
            new UserRepository(context),
            new NotebookRepository(context),
            new PageRepository(context),
            new BlockRepository(context),
            new BlockRevisionRepository(context),
            new TestChangeSourceAccessor(),
            context
        );

    private sealed class TestCurrentUserAccessor(Guid userId) : ICurrentUserAccessor
    {
        public CurrentUser? User { get; } = new(userId);
    }

    private sealed class TestChangeSourceAccessor : IChangeSourceAccessor
    {
        public RevisionSource Source { get; set; } = RevisionSource.Human;
    }
}
