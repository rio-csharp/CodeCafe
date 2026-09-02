using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes.Enums;

namespace CodeCafe.Infrastructure.Tests;

[Collection("postgres")]
public sealed class NotebookReadServiceTests : IDisposable
{
    private readonly NotesDbHarness _harness;

    public NotebookReadServiceTests(PostgresFixture fixture)
    {
        _harness = new NotesDbHarness(fixture);
    }

    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetMyNotebooks_Returns_Owners_Notebooks_With_Metadata()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(context, OwnerId, "Architecture Notes", "architecture-notes");
        _harness.SeedPage(context, notebook, "Overview");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetMyNotebooksAsync(OwnerId, null, CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.Equal("architecture-notes", summary.Slug);
        Assert.Equal("Alice", summary.AuthorDisplayName);
        Assert.Equal(1, summary.PageCount);
        Assert.True(summary.CanEdit);
        Assert.Equal("private", summary.Visibility);
    }

    [Fact]
    public async Task GetNotebookBySlug_Private_Notebook_Is_Forbidden_For_Stranger()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedNotebook(context, OwnerId, "Secret", "secret");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync("secret", OtherUserId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.Forbidden, result.Error!.Kind);
        Assert.Equal("notebook_forbidden", result.Error.Code);
    }

    [Fact]
    public async Task GetNotebookBySlug_Public_Notebook_Is_Readable_By_Stranger()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedNotebook(context, OwnerId, "Public Notes", "public-notes", NotebookVisibility.Public);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync("public-notes", OtherUserId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.CanEdit);
    }

    [Fact]
    public async Task GetNotebookBySlug_Shared_Private_Notebook_Is_Readable_By_Recipient()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedUser(context, OtherUserId, "Bob");
        var notebook = _harness.SeedNotebook(context, OwnerId, "Team Notes", "team-notes");
        _harness.SeedShare(context, notebook.Id, OtherUserId, OwnerId);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync("team-notes", OtherUserId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.CanEdit);
    }

    [Fact]
    public async Task GetNotebookBySlug_Unknown_Slug_Returns_NotFound()
    {
        using var context = _harness.CreateContext();
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync("missing", OwnerId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task GetNotebookItemByPath_Returns_Page_With_Content()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(context, OwnerId, "Notes", "notes");
        _harness.SeedPage(context, notebook, "Overview", "overview");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookItemByPathAsync("notes", "overview", OwnerId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("overview", result.Value!.Path);
        Assert.Equal("page", result.Value.Type);
    }

    [Fact]
    public async Task GetNotebookItemByPath_Blank_Path_Returns_NotFound()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedNotebook(context, OwnerId, "Notes", "notes");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookItemByPathAsync("notes", "  ", OwnerId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(NotesFailureKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task SearchVisibleNotebookItems_Includes_Own_Public_And_Shared()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedUser(context, OtherUserId, "Bob");

        var ownNotebook = _harness.SeedNotebook(context, OwnerId, "Mine", "mine");
        _harness.SeedPage(context, ownNotebook, "Caching Guide");

        var publicNotebook = _harness.SeedNotebook(context, OtherUserId, "Public", "public-one", NotebookVisibility.Public);
        _harness.SeedPage(context, publicNotebook, "Caching Tips");

        var sharedNotebook = _harness.SeedNotebook(context, OtherUserId, "Shared", "shared-one");
        _harness.SeedPage(context, sharedNotebook, "Caching Notes");
        _harness.SeedShare(context, sharedNotebook.Id, OwnerId, OtherUserId);

        var hiddenNotebook = _harness.SeedNotebook(context, OtherUserId, "Hidden", "hidden-one");
        _harness.SeedPage(context, hiddenNotebook, "Caching Secrets");

        var readService = _harness.CreateReadService(context);

        var results = await readService.SearchVisibleNotebookItemsAsync(OwnerId, "caching", CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.DoesNotContain(results, item => item.Path == "caching-secrets");
    }

    [Fact]
    public async Task GetNotebookFavoriteStatus_Returns_Counts_And_Access_Rules()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(context, OwnerId, "Notes", "notes");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookFavoriteStatusAsync(notebook.Id, OwnerId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.IsFavorited);
        Assert.Equal(0, result.Value.FavoriteCount);

        var forbidden = await readService.GetNotebookFavoriteStatusAsync(notebook.Id, OtherUserId, CancellationToken.None);
        Assert.False(forbidden.Succeeded);
        Assert.Equal(NotesFailureKind.Forbidden, forbidden.Error!.Kind);
    }

    [Fact]
    public async Task GetFavoriteNotebooks_Returns_Only_Favorited_Ones()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var favorited = _harness.SeedNotebook(context, OwnerId, "Fav Notes", "fav-notes");
        _harness.SeedNotebook(context, OwnerId, "Other Notes", "other-notes");
        _harness.SeedFavorite(context, favorited.Id, OwnerId);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetFavoriteNotebooksAsync(OwnerId, null, CancellationToken.None);

        var model = Assert.Single(result);
        Assert.Equal("fav-notes", model.Slug);
        Assert.True(model.IsFavoritedByMe);
    }

    [Fact]
    public async Task GetFavoriteNotebooks_Includes_Favorited_Public_From_Stranger()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OtherUserId, "Bob");
        _harness.SeedUser(context, OwnerId, "Alice");
        var publicNotebook = _harness.SeedNotebook(
            context, OtherUserId, "Bob Public", "bob-public", NotebookVisibility.Public);
        _harness.SeedFavorite(context, publicNotebook.Id, OwnerId);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetFavoriteNotebooksAsync(OwnerId, null, CancellationToken.None);

        var model = Assert.Single(result);
        Assert.Equal("bob-public", model.Slug);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task GetFavoriteNotebooks_Excludes_Strangers_Private_Even_If_Favorited()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OtherUserId, "Bob");
        _harness.SeedUser(context, OwnerId, "Alice");
        var privateNotebook = _harness.SeedNotebook(context, OtherUserId, "Bob Private", "bob-private");
        _harness.SeedFavorite(context, privateNotebook.Id, OwnerId);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetFavoriteNotebooksAsync(OwnerId, null, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFavoriteNotebooks_Includes_Favorited_Shared_Notebook()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OtherUserId, "Bob");
        _harness.SeedUser(context, OwnerId, "Alice");
        var shared = _harness.SeedNotebook(context, OtherUserId, "Shared Notes", "shared-notes");
        _harness.SeedShare(context, shared.Id, OwnerId, OtherUserId);
        _harness.SeedFavorite(context, shared.Id, OwnerId);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetFavoriteNotebooksAsync(OwnerId, null, CancellationToken.None);

        var model = Assert.Single(result);
        Assert.Equal("shared-notes", model.Slug);
    }

    [Fact]
    public async Task GetNotebookBySlug_Coded_Unlisted_Without_Code_Requires_AccessCode()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(
            context, OwnerId, "Secret Notes", "secret-notes", NotebookVisibility.Unlisted);
        _harness.SeedAccessCode(context, notebook, "s3cret");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync(
            "secret-notes", Guid.Empty, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("access_code_required", result.Error!.Code);
    }

    [Fact]
    public async Task GetNotebookBySlug_Coded_Unlisted_With_Wrong_Code_Requires_AccessCode()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(
            context, OwnerId, "Secret Notes", "secret-notes", NotebookVisibility.Unlisted);
        _harness.SeedAccessCode(context, notebook, "s3cret");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync(
            "secret-notes", Guid.Empty, CancellationToken.None, accessCode: "wrong");

        Assert.False(result.Succeeded);
        Assert.Equal("access_code_required", result.Error!.Code);
    }

    [Fact]
    public async Task GetNotebookBySlug_Coded_Unlisted_With_Right_Code_Succeeds()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(
            context, OwnerId, "Secret Notes", "secret-notes", NotebookVisibility.Unlisted);
        _harness.SeedAccessCode(context, notebook, "s3cret");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync(
            "secret-notes", Guid.Empty, CancellationToken.None, accessCode: "s3cret");

        Assert.True(result.Succeeded);
        Assert.Equal("secret-notes", result.Value!.Slug);
    }

    [Fact]
    public async Task GetNotebookBySlug_Coded_Unlisted_Owner_Needs_No_Code()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(
            context, OwnerId, "Secret Notes", "secret-notes", NotebookVisibility.Unlisted);
        _harness.SeedAccessCode(context, notebook, "s3cret");
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync(
            "secret-notes", OwnerId, CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task GetNotebookBySlug_Plain_Unlisted_Still_Open_Anonymously()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedNotebook(
            context, OwnerId, "Link Notes", "link-notes", NotebookVisibility.Unlisted);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync(
            "link-notes", Guid.Empty, CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task GetNotebookBySlug_Coded_Unlisted_Shared_User_Needs_No_Code()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        _harness.SeedUser(context, OtherUserId, "Bob");
        var notebook = _harness.SeedNotebook(
            context, OwnerId, "Secret Notes", "secret-notes", NotebookVisibility.Unlisted);
        _harness.SeedAccessCode(context, notebook, "s3cret");
        _harness.SeedShare(context, notebook.Id, OtherUserId, OwnerId);
        var readService = _harness.CreateReadService(context);

        var result = await readService.GetNotebookBySlugAsync(
            "secret-notes", OtherUserId, CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Search_Returns_Match_Window_Snippet()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(context, OwnerId, "Notes", "notes");
        var pageId = _harness.SeedPage(context, notebook, "Long Page");
        var longText = new string('x', 500) + "needle" + new string('y', 500);
        _harness.SeedPageContent(context, pageId, longText);
        var readService = _harness.CreateReadService(context);

        var result = await readService.SearchVisibleNotebookItemsAsync(
            OwnerId, "needle", CancellationToken.None);

        var model = Assert.Single(result);
        Assert.Contains("needle", model.Snippet);
        Assert.True(model.Snippet!.Length <= 400, $"snippet was {model.Snippet!.Length} chars");
        Assert.DoesNotContain(new string('x', 300), model.Snippet);
    }

    [Fact]
    public async Task Search_Title_Only_Match_Falls_Back_To_Page_Head()
    {
        using var context = _harness.CreateContext();
        _harness.SeedUser(context, OwnerId, "Alice");
        var notebook = _harness.SeedNotebook(context, OwnerId, "Notes", "notes");
        var pageId = _harness.SeedPage(context, notebook, "Needle In Title");
        _harness.SeedPageContent(context, pageId, "body starts here and has no hit");
        var readService = _harness.CreateReadService(context);

        var result = await readService.SearchVisibleNotebookItemsAsync(
            OwnerId, "needle", CancellationToken.None);

        var model = Assert.Single(result);
        Assert.StartsWith("body starts here", model.Snippet);
    }

    public void Dispose() => _harness.Dispose();
}
