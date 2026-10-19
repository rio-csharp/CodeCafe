using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.PreparePageImport;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Pages;

public sealed class PreparePageImportCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAGrantBoundToTheNotebookAndPath()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Stage 1", "stage-1", "a");
        var pages = new StubPageRepository();
        pages.Add(parent);
        var handler = CreateHandler(owner.Id, notebook, pages);

        var result = await handler.Handle(
            new PreparePageImportCommand(notebook.Slug, @"C:\notes\001-async-await.md", 4096, "/stage-1/"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var grant = result.Value!;
        Assert.Equal(ImportUploadKind.Page, grant.Kind);
        Assert.Equal(owner.Id, grant.UserId);
        Assert.Equal(notebook.Id, grant.NotebookId);
        Assert.Equal("/stage-1", grant.ParentPath);
        Assert.Equal("001-async-await.md", grant.FileName);
        Assert.Equal(4096, grant.MaxBytes);
    }

    [Fact]
    public async Task Handle_TreatsSlashAsTheNotebookRoot()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner.Id, notebook, new StubPageRepository());

        var result = await handler.Handle(
            new PreparePageImportCommand(notebook.Slug, "note.md", 1024, "/"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ParentPath);
    }

    [Fact]
    public async Task Handle_RejectsAMissingParent()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner.Id, notebook, new StubPageRepository());

        var result = await handler.Handle(
            new PreparePageImportCommand(notebook.Slug, "note.md", 1024, "/missing"),
            CancellationToken.None
        );

        Assert.Equal(PageErrors.ParentNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_RejectsANonWriter()
    {
        var owner = SeedOwner();
        var viewer = User.Create("viewer@example.com", "viewer@example.com", "Viewer", "hash");
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(viewer.Id, notebook, new StubPageRepository());

        var result = await handler.Handle(
            new PreparePageImportCommand(notebook.Slug, "note.md", 1024),
            CancellationToken.None
        );

        Assert.Equal(NotebookErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_RejectsANonPositiveLimit()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner.Id, notebook, new StubPageRepository());

        var result = await handler.Handle(
            new PreparePageImportCommand(notebook.Slug, "note.md", 0),
            CancellationToken.None
        );

        Assert.Equal(ImportUploadErrors.InvalidMaxBytes, result.Error);
    }

    [Fact]
    public async Task Handle_RejectsAnEmptyFileName()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var handler = CreateHandler(owner.Id, notebook, new StubPageRepository());

        var result = await handler.Handle(
            new PreparePageImportCommand(notebook.Slug, @"C:\", 1024),
            CancellationToken.None
        );

        Assert.Equal(PageErrors.InvalidImport, result.Error);
    }

    private static PreparePageImportCommandHandler CreateHandler(
        Guid userId,
        Notebook notebook,
        StubPageRepository pages
    ) => new(new StubCurrentUserAccessor(new CurrentUser(userId)), new StubNotebookRepository { notebook }, pages);

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);
}
