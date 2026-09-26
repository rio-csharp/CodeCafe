using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.UpdatePage;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests;

public sealed class UpdatePageCommandHandlerTests
{
    [Fact]
    public async Task Handle_RenamesAndArchives_SlugStaysStable()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "Old", "old", "a");
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(
            new UpdatePageCommand(page.Id, "  New Title  ", IsArchived: true),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", page.Title);
        Assert.Equal("old", page.Slug); // renaming keeps the slug: paths stay stable
        Assert.True(page.IsArchived);
        Assert.Equal("/old", result.Value!.Path);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_NullFields_KeepCurrentValues()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "Keep", "keep", "a");
        var (handler, _) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new UpdatePageCommand(page.Id, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Keep", page.Title);
        Assert.False(page.IsArchived);
    }

    [Fact]
    public async Task Handle_AllowsEditorViaPageShare()
    {
        var owner = SeedOwner();
        var editor = User.Create("editor@example.com", "editor@example.com", "Editor", "hash");
        var notebook = SeedNotebook(owner); // private, the editor has no notebook access
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(editor.Id, CollaboratorRole.Editor);
        var (handler, _) = CreateHandler(editor.Id, notebook, page);

        var result = await handler.Handle(new UpdatePageCommand(page.Id, "Renamed", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", page.Title);
    }

    [Fact]
    public async Task Handle_DeniesViewerViaPageShare()
    {
        var owner = SeedOwner();
        var viewer = User.Create("viewer@example.com", "viewer@example.com", "Viewer", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "Shared", "shared", "a");
        page.Share(viewer.Id, CollaboratorRole.Viewer);
        var (handler, _) = CreateHandler(viewer.Id, notebook, page);

        var result = await handler.Handle(new UpdatePageCommand(page.Id, "Nope", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Equal("Shared", page.Title);
    }

    [Fact]
    public async Task Handle_DeniesStranger()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        var (handler, _) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(new UpdatePageCommand(page.Id, "Nope", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (UpdatePageCommandHandler Handler, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var unitOfWork = new StubUnitOfWork();
        return (
            new UpdatePageCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                new StubUserRepository(),
                unitOfWork
            ),
            unitOfWork
        );
    }
}
