using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.ChangePageSlug;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class ChangePageSlugCommandHandlerTests
{
    [Fact]
    public async Task Handle_DerivesFromTheTitle_WhenNoSlugIsGiven()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var parent = Page.Create(notebook.Id, null, "Setup", "setup", "a");
        var page = ProvisionalPage(notebook, parent.Id, "Café Notes");
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook, parent, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("café-notes", page.Slug);
        // The path is rebuilt from the ancestor chain, so a nested page reports its full address.
        Assert.Equal("/setup/café-notes", result.Value!.Path);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_UsesThePickedSlug()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = ProvisionalPage(notebook, null, "Café Notes");
        var (handler, _) = CreateHandler(owner.Id, notebook, page);

        // Padded and upper-cased: the handler normalizes before storing.
        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, "  Filter-Notes  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("filter-notes", page.Slug);
        Assert.Equal("/filter-notes", result.Value!.Path);
    }

    [Fact]
    public async Task Handle_ReturnsSlugAlreadyTaken_ForAPickedSlug()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var taken = Page.Create(notebook.Id, null, "Filter Notes", "filter-notes", "a");
        var page = ProvisionalPage(notebook, null, "Café Notes");
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook, taken, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, "filter-notes"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.SlugAlreadyTaken, result.Error);
        // The caller picked it, so nothing is written and nothing is re-keyed.
        Assert.Equal("untitled", page.Slug);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsSlugAlreadyTaken_WhenAPickedSlugRaces()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = ProvisionalPage(notebook, null, "Café Notes");
        var (handler, _) = RacingHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, "filter-notes"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.SlugAlreadyTaken, result.Error);
        // Kept as picked: no random suffix, because the caller owns this slug.
        Assert.Equal("filter-notes", page.Slug);
    }

    [Fact]
    public async Task Handle_SuffixesTheDerivedSlug_WhenATitleTwinHoldsIt()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var twin = Page.Create(notebook.Id, null, "Café Notes", "café-notes", "a");
        var page = ProvisionalPage(notebook, null, "Café Notes");
        var (handler, _) = CreateHandler(owner.Id, notebook, twin, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("café-notes-", page.Slug);
    }

    [Fact]
    public async Task Handle_RekeysTheDerivedSlug_WhenSaveRaces()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = ProvisionalPage(notebook, null, "Race Notes");
        var (handler, unitOfWork) = RacingHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual("race-notes", page.Slug);
        Assert.StartsWith("race-notes-", page.Slug);
    }

    [Fact]
    public async Task Handle_KeepsThePageSlug_WhenItAlreadyMatchesTheTitle()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "Untitled", "untitled", "a");
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Regenerating here would suffix the page for holding its own slug.
        Assert.Equal("untitled", page.Slug);
        Assert.Equal("/untitled", result.Value!.Path);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_KeepsThePageSlug_WhenThePickedSlugIsTheCurrentOne()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner);
        var page = Page.Create(notebook.Id, null, "Something", "something", "a");
        var (handler, unitOfWork) = CreateHandler(owner.Id, notebook, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, "something"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("something", page.Slug);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_DeniesViewerViaPageShare()
    {
        var owner = SeedOwner();
        var viewer = User.Create("viewer@example.com", "viewer@example.com", "Viewer", "hash");
        var notebook = SeedNotebook(owner);
        var page = ProvisionalPage(notebook, null, "Café Notes");
        page.Share(viewer.Id, CollaboratorRole.Viewer);
        var (handler, _) = CreateHandler(viewer.Id, notebook, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, "nope"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
        Assert.Equal("untitled", page.Slug);
    }

    [Fact]
    public async Task Handle_DeniesStranger()
    {
        var owner = SeedOwner();
        var stranger = User.Create("stranger@example.com", "stranger@example.com", "Stranger", "hash");
        var notebook = SeedNotebook(owner);
        var page = ProvisionalPage(notebook, null, "Café Notes");
        var (handler, _) = CreateHandler(stranger.Id, notebook, page);

        var result = await handler.Handle(new ChangePageSlugCommand(page.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.NotFound, result.Error);
    }

    // The reader renames first and repairs the slug after, so the page under test is a page that
    // still carries its provisional slug.
    private static Page ProvisionalPage(Notebook notebook, Guid? parentId, string renamedTitle)
    {
        var page = Page.Create(notebook.Id, parentId, "Untitled", "untitled", "a");
        page.Rename(renamedTitle);
        return page;
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner)
        => Notebook.Create(owner.Id, "Notebook", null, "my-notebook", NotebookVisibility.Private);

    private static (ChangePageSlugCommandHandler Handler, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        return CreateHandler(currentUserId, notebook, pages, new StubUnitOfWork());
    }

    // One lost slug race before the save sticks, mirroring a concurrent insert taking the slug.
    private static (ChangePageSlugCommandHandler Handler, StubUnitOfWork UnitOfWork) RacingHandler(
        Guid currentUserId,
        Notebook notebook,
        params Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var unitOfWork = new StubUnitOfWork(new UniqueConstraintViolationException("IX_pages_NotebookId_Slug"))
        {
            FailuresRemaining = 1,
        };
        return CreateHandler(currentUserId, notebook, pages, unitOfWork);
    }

    private static (ChangePageSlugCommandHandler Handler, StubUnitOfWork UnitOfWork) CreateHandler(
        Guid currentUserId,
        Notebook notebook,
        StubPageRepository pages,
        StubUnitOfWork unitOfWork
    )
        => (
            new ChangePageSlugCommandHandler(
                new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
                new StubNotebookRepository { notebook },
                pages,
                new StubUserRepository(),
                unitOfWork
            ),
            unitOfWork
        );
}
