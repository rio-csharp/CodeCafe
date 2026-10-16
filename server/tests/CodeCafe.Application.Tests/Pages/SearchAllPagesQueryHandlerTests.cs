using CodeCafe.Application.Auth;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.SearchAllPages;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Tests.Pages;

public sealed class SearchAllPagesQueryHandlerTests
{
    [Fact]
    public async Task Handle_OwnerSeesMatches_FromOwnNotebook()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var page = SeedPage(notebook, "Rust ownership", "rust-ownership");
        var (pages, _) = Seed([notebook], [page]);

        var result = await CreateHandler(owner, pages).Handle(Query("rust"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Value!.Items);
        Assert.Equal(page.Id, hit.PageId);
        Assert.Equal(notebook.Id, hit.NotebookId);
        Assert.Equal(notebook.Slug, hit.NotebookSlug);
        Assert.Equal(notebook.Title, hit.NotebookTitle);
        Assert.Equal("/rust-ownership", hit.Path);
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task Handle_CollaboratorSeesMatches_NonMemberDoesNot()
    {
        var owner = SeedUser("owner@example.com");
        var collaborator = SeedUser("collaborator@example.com");
        var stranger = SeedUser("stranger@example.com");
        var notebook = SeedNotebook(owner, "notes");
        notebook.Share(collaborator.Id, CollaboratorRole.Viewer);
        var page = SeedPage(notebook, "Rust ownership", "rust-ownership");
        var (pages, _) = Seed([notebook], [page]);

        var collaboratorResult = await CreateHandler(collaborator, pages).Handle(Query("rust"), CancellationToken.None);
        var strangerResult = await CreateHandler(stranger, pages).Handle(Query("rust"), CancellationToken.None);

        Assert.True(collaboratorResult.IsSuccess);
        Assert.Single(collaboratorResult.Value!.Items);
        Assert.True(strangerResult.IsSuccess);
        Assert.Empty(strangerResult.Value!.Items);
    }

    [Fact]
    public async Task Handle_PageShareAlone_DoesNotSurfaceThePage()
    {
        // Page-share subtrees are a deliberate v1 exclusion: a page-level share on its own
        // must not make the page searchable.
        var owner = SeedUser("owner@example.com");
        var pageSharedUser = SeedUser("page-share@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var page = SeedPage(notebook, "Rust ownership", "rust-ownership");
        page.Share(pageSharedUser.Id, CollaboratorRole.Viewer);
        var (pages, _) = Seed([notebook], [page]);

        var result = await CreateHandler(pageSharedUser, pages).Handle(Query("rust"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task Handle_TitleOnlyMatch_HasEmptySnippet()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var page = SeedPage(notebook, "Rust ownership", "rust-ownership");
        var (pages, _) = Seed([notebook], [page]);

        var result = await CreateHandler(owner, pages).Handle(Query("ownership"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, Assert.Single(result.Value!.Items).Snippet);
    }

    [Fact]
    public async Task Handle_ContentMatch_ProducesExcerptWithEllipses()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var page = SeedPage(notebook, "Plain title", "plain-title");
        var plainText = new string('a', 200) + " needle " + new string('b', 200);
        var block = Block.Create(page.Id, null, "paragraph", """{"spans":[]}""", plainText, "a0");
        var (pages, _) = Seed([notebook], [page], [block]);

        var result = await CreateHandler(owner, pages).Handle(Query("NEEDLE"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var snippet = Assert.Single(result.Value!.Items).Snippet;
        Assert.StartsWith("…", snippet);
        Assert.EndsWith("…", snippet);
        Assert.Contains("needle", snippet);
        // ±60 chars of context around the 6-char match, plus the two ellipses.
        Assert.Equal(128, snippet.Length);
    }

    [Fact]
    public async Task Handle_Snippet_CollapsesWhitespace()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var page = SeedPage(notebook, "Plain title", "plain-title");
        var block = Block.Create(page.Id, null, "paragraph", """{"spans":[]}""", "hello\n\n  needle\t world", "a0");
        var (pages, _) = Seed([notebook], [page], [block]);

        var result = await CreateHandler(owner, pages).Handle(Query("needle"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("hello needle world", Assert.Single(result.Value!.Items).Snippet);
    }

    [Fact]
    public async Task Handle_ExcludesTrashedPages()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var live = SeedPage(notebook, "Rust live", "rust-live");
        var trashed = SeedPage(notebook, "Rust gone", "rust-gone");
        trashed.SoftDelete(DateTimeOffset.UtcNow);
        var (pages, _) = Seed([notebook], [live, trashed]);

        var result = await CreateHandler(owner, pages).Handle(Query("rust"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(live.Id, Assert.Single(result.Value!.Items).PageId);
    }

    [Fact]
    public async Task Handle_ExcludesPagesOfTrashedNotebooks()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var page = SeedPage(notebook, "Rust ownership", "rust-ownership");
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        var (pages, _) = Seed([notebook], [page]);

        var result = await CreateHandler(owner, pages).Handle(Query("rust"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenNoCurrentUser()
    {
        var handler = new SearchAllPagesQueryHandler(new StubCurrentUserAccessor(null), new StubPageRepository());

        var result = await handler.Handle(Query("rust"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_RejectsMalformedCursor()
    {
        var owner = SeedUser("owner@example.com");
        var (pages, _) = Seed([], []);

        var result = await CreateHandler(owner, pages).Handle(
            Query("rust") with { Cursor = "not-a-cursor" },
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(PageErrors.InvalidCursor, result.Error);
    }

    [Fact]
    public async Task Handle_CursorPaging_ReturnsNoOverlap()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var oldest = SeedPage(notebook, "Rust one", "rust-one");
        var middle = SeedPage(notebook, "Rust two", "rust-two");
        var newest = SeedPage(notebook, "Rust three", "rust-three");
        // Touch in order so UpdatedAtUtc ordering is deterministic regardless of creation timing.
        oldest.Touch();
        middle.Touch();
        newest.Touch();
        var (pages, _) = Seed([notebook], [oldest, middle, newest]);
        var handler = CreateHandler(owner, pages);

        var firstPage = await handler.Handle(Query("rust") with { PageSize = 2 }, CancellationToken.None);
        Assert.True(firstPage.IsSuccess);
        var expectedOrder = new[] { oldest, middle, newest }
            .OrderByDescending(page => page.UpdatedAtUtc)
            .ThenByDescending(page => page.Id)
            .Select(page => page.Id)
            .ToArray();
        Assert.Equal(expectedOrder[..2], firstPage.Value!.Items.Select(item => item.PageId));
        Assert.NotNull(firstPage.Value.NextCursor);

        var secondPage = await handler.Handle(
            Query("rust") with { PageSize = 2, Cursor = firstPage.Value.NextCursor },
            CancellationToken.None
        );

        Assert.True(secondPage.IsSuccess);
        Assert.Equal(expectedOrder[2..], secondPage.Value!.Items.Select(item => item.PageId));
        Assert.Null(secondPage.Value.NextCursor);
        Assert.Empty(
            secondPage.Value.Items.Select(item => item.PageId).Intersect(firstPage.Value.Items.Select(item => item.PageId))
        );
    }

    [Fact]
    public async Task Handle_ClampsPageSize()
    {
        var owner = SeedUser("owner@example.com");
        var notebook = SeedNotebook(owner, "notes");
        var pagesToSeed = Enumerable.Range(0, 3).Select(index => SeedPage(notebook, $"Rust {index}", $"rust-{index}")).ToList();
        var (pages, _) = Seed([notebook], pagesToSeed);

        var result = await CreateHandler(owner, pages).Handle(Query("rust") with { PageSize = 500 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Items.Count);
    }

    private static SearchAllPagesQuery Query(string query) => new(query, null, null);

    private static SearchAllPagesQueryHandler CreateHandler(User currentUser, StubPageRepository pages)
        => new(new StubCurrentUserAccessor(new CurrentUser(currentUser.Id)), pages);

    private static User SeedUser(string email) => User.Create(email, email, "User", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, $"Title {slug}", null, slug, NotebookVisibility.Private);

    private static Page SeedPage(Notebook notebook, string title, string slug)
        => Page.Create(notebook.Id, null, title, slug, $"s-{slug}");

    private static (StubPageRepository Pages, StubNotebookRepository Notebooks) Seed(
        IEnumerable<Notebook> notebooks,
        IEnumerable<Page> pages,
        IEnumerable<Block>? blocks = null
    )
    {
        var notebookRepository = new StubNotebookRepository();
        notebookRepository.AddRange(notebooks);
        var pageRepository = new StubPageRepository(notebookRepository);
        pageRepository.AddRange(pages);
        if (blocks is not null)
        {
            pageRepository.Blocks.AddRange(blocks);
        }

        return (pageRepository, notebookRepository);
    }
}
