using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Application.Pages.ImportPage;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class ImportPageCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesPageWithBlocks_AtNotebookRoot()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, blocks, revisions, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# Getting Started\n\nHello **there**")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var page = Assert.Single(pages);
        Assert.Equal("Getting Started", page.Title);
        Assert.Equal("getting-started", page.Slug);
        Assert.Null(page.ParentId);
        Assert.Equal(page.Id, notebook.FirstPageId);
        Assert.Equal("/getting-started", result.Value!.Path);

        var block = Assert.Single(blocks);
        Assert.Equal(page.Id, block.PageId);
        Assert.Equal("Hello there", block.PlainText);

        var revision = Assert.Single(revisions);
        Assert.Equal(block.Id, revision.BlockId);
        Assert.Equal(BlockChangeKind.Added, revision.ChangeKind);
        Assert.Equal(RevisionSource.Human, revision.Source);
    }

    [Fact]
    public async Task Handle_RecreatesNestedBlocks_AsOneBatch()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, _, blocks, revisions, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# T\n\n- parent\n  - child")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, blocks.Count);
        var parent = blocks.Single(block => block.PlainText == "parent");
        var child = blocks.Single(block => block.PlainText == "child");
        Assert.Equal(parent.Id, child.ParentBlockId);
        Assert.Equal(child.Id, parent.FirstChildId);
        Assert.Equal(2, revisions.Count);
        Assert.Single(revisions.Select(revision => revision.BatchId).Distinct());
    }

    [Fact]
    public async Task Handle_NoHeading_FallsBackToFileName()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, _, _, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("meeting-notes.md", "just content")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("meeting-notes", Assert.Single(pages).Title);
    }

    [Fact]
    public async Task Handle_NoTitleAnywhere_ReturnsInvalidImport()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, _, _, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("", "just content")),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.InvalidImport, result.Error);
        Assert.Empty(pages);
    }

    [Fact]
    public async Task Handle_LongTitle_IsTruncated()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, _, _, _) = CreateHandler(owner.Id, notebook);
        var longTitle = new string('x', Page.MaxTitleLength + 50);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", $"# {longTitle}")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(Page.MaxTitleLength, Assert.Single(pages).Title.Length);
    }

    [Fact]
    public async Task Handle_WithParentPath_CreatesChildPage()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var (handler, pages, _, _, _) = CreateHandler(owner.Id, notebook, parent);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# Child"), "/parent"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var child = Assert.Single(pages, page => page.Id != parent.Id);
        Assert.Equal(parent.Id, child.ParentId);
        Assert.Equal(child.Id, parent.FirstChildId);
        Assert.Equal("/parent/child", result.Value!.Path);
    }

    [Fact]
    public async Task Handle_UnknownParentPath_ReturnsParentNotFound()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, _, _, _) = CreateHandler(owner.Id, notebook);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# Child"), "/missing"),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.ParentNotFound, result.Error);
        Assert.Empty(pages);
    }

    [Fact]
    public async Task Handle_AppendsAfterLastSibling()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var first = Page.Create(notebook.Id, null, "First", "first", "a");
        var (handler, pages, _, _, _) = CreateHandler(owner.Id, notebook, first);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# Second")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var second = Assert.Single(pages, page => page.Title == "Second");
        Assert.Equal(second.Id, first.NextSiblingId);
    }

    [Fact]
    public async Task Handle_RetriesWithFreshSlug_WhenSaveRaces()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, pages, _, _, unitOfWork) = CreateHandler(owner.Id, notebook);
        unitOfWork.FailuresRemaining = 1;

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# Race")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var page = Assert.Single(pages);
        Assert.NotEqual("race", page.Slug);
        Assert.StartsWith("race-", page.Slug);
    }

    [Fact]
    public async Task Handle_DeniesViewerCollaborator()
    {
        var owner = SeedOwner();
        var viewer = User.Create("viewer@example.com", "viewer@example.com", "Viewer", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(viewer.Id, CollaboratorRole.Viewer);
        var (handler, pages, _, _, _) = CreateHandler(viewer.Id, notebook);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# Nope")),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(NotebookErrors.NotFound, result.Error);
        Assert.Empty(pages);
    }

    [Fact]
    public async Task Handle_AllowsEditorCollaborator()
    {
        var owner = SeedOwner();
        var editor = User.Create("editor@example.com", "editor@example.com", "Editor", "hash");
        var notebook = SeedNotebook(owner);
        notebook.Share(editor.Id, CollaboratorRole.Editor);
        var (handler, pages, _, _, _) = CreateHandler(editor.Id, notebook);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# Draft")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Single(pages);
    }

    [Fact]
    public async Task Handle_AiSource_FlowsIntoRevisions()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var (handler, _, _, revisions, _) = CreateHandler(owner.Id, notebook, RevisionSource.Ai);

        var result = await handler.Handle(
            new ImportPageCommand(notebook.Slug, new PageExportDto("x.md", "# T\n\ncontent")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.All(revisions, revision => Assert.Equal(RevisionSource.Ai, revision.Source));
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (
        ImportPageCommandHandler Handler,
        StubPageRepository Pages,
        StubBlockRepository Blocks,
        StubBlockRevisionRepository Revisions,
        StubUnitOfWork UnitOfWork
    ) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    ) => CreateHandler(currentUserId, notebook, RevisionSource.Human, seededPages);

    private static (
        ImportPageCommandHandler Handler,
        StubPageRepository Pages,
        StubBlockRepository Blocks,
        StubBlockRevisionRepository Revisions,
        StubUnitOfWork UnitOfWork
    ) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        RevisionSource source,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var blocks = new StubBlockRepository();
        var revisions = new StubBlockRevisionRepository();
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_pages_NotebookId_Slug"))
        {
            FailuresRemaining = 0,
        };
        return (
            new ImportPageCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                blocks,
                revisions,
                new StubChangeSourceAccessor { Source = source },
                unitOfWork
            ),
            pages,
            blocks,
            revisions,
            unitOfWork
        );
    }
}
