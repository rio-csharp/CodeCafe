using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;

namespace CodeCafe.Infrastructure.Tests;

public sealed class NotebookReadServiceTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PublicNotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PrivateNotebookId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PageItemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid FolderItemId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static NotesDbHarness CreateSeededHarness()
    {
        var harness = new NotesDbHarness();
        using var seed = harness.CreateContext();

        seed.AddUser(OwnerId, "Yao");
        seed.AddUser(OtherUserId, "Mei");

        seed.AddNotebook(PublicNotebookId, OwnerId, "Architecture Notes", "architecture-notes", NotebookVisibility.Public, true);
        seed.AddNotebook(PrivateNotebookId, OwnerId, "Secret Notes", "secret-notes", NotebookVisibility.Private, false);

        seed.AddItem(FolderItemId, PublicNotebookId, NotebookItemType.Folder, "Chapters", "chapters", 0);
        seed.AddItem(PageItemId, PublicNotebookId, NotebookItemType.Page, "Overview", "chapters/overview", 1, parentId: FolderItemId, plainTextContent: "Hexagonal architecture overview");
        seed.AddItem(Guid.NewGuid(), PublicNotebookId, NotebookItemType.Page, "Archived Page", "chapters/archived", 2, parentId: FolderItemId, plainTextContent: "old", isArchived: true);

        seed.AddFavorite(PublicNotebookId, OtherUserId);
        seed.SaveChanges();

        return harness;
    }

    [Fact]
    public async Task GetPublicNotebooks_ReturnsOnlyPublishedPublic_WithMetadata()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var result = await service.GetPublicNotebooksAsync(null, OtherUserId, CancellationToken.None);

        var notebook = Assert.Single(result);
        Assert.Equal("architecture-notes", notebook.Slug);
        Assert.Equal("Yao", notebook.AuthorDisplayName);
        // Two non-archived items (folder + page); archived item is excluded.
        Assert.Equal(2, notebook.ItemCount);
        Assert.Equal(1, notebook.FolderCount);
        Assert.Equal(1, notebook.PageCount);
        Assert.Equal(1, notebook.FavoriteCount);
        Assert.True(notebook.IsFavoritedByMe);
        Assert.False(notebook.CanEdit);
    }

    [Fact]
    public async Task GetPublicNotebooks_FiltersBySearch()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        Assert.Single(await service.GetPublicNotebooksAsync("architecture", OtherUserId, CancellationToken.None));
        Assert.Empty(await service.GetPublicNotebooksAsync("nonexistent", OtherUserId, CancellationToken.None));
    }

    [Fact]
    public async Task GetPublicNotebooks_AppliesLimit()
    {
        using var harness = new NotesDbHarness();
        await using (var seed = harness.CreateContext())
        {
            seed.AddUser(OwnerId, "Yao");
            seed.AddNotebook(Guid.NewGuid(), OwnerId, "Alpha", "alpha", NotebookVisibility.Public, true);
            seed.AddNotebook(Guid.NewGuid(), OwnerId, "Beta", "beta", NotebookVisibility.Public, true);
            seed.AddNotebook(Guid.NewGuid(), OwnerId, "Gamma", "gamma", NotebookVisibility.Public, true);
            await seed.SaveChangesAsync();
        }

        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        Assert.Equal(2, (await service.GetPublicNotebooksAsync(null, OtherUserId, CancellationToken.None, limit: 2)).Count);
    }

    [Fact]
    public async Task GetMyNotebooks_ReturnsOwnedOnly_IncludingUnpublished()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var owned = await service.GetMyNotebooksAsync(OwnerId, null, CancellationToken.None);

        Assert.Equal(2, owned.Count);
        Assert.All(owned, notebook => Assert.True(notebook.CanEdit));
        Assert.Empty(await service.GetMyNotebooksAsync(OtherUserId, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchVisibleNotebookItems_MatchesTitleOrContent_ExcludesArchived()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var byTitle = await service.SearchVisibleNotebookItemsAsync(OtherUserId, "Overview", CancellationToken.None);
        Assert.Single(byTitle);

        var byContent = await service.SearchVisibleNotebookItemsAsync(OtherUserId, "Hexagonal", CancellationToken.None);
        Assert.Single(byContent);

        // "old" lives only on the archived item, which must not surface.
        Assert.Empty(await service.SearchVisibleNotebookItemsAsync(OtherUserId, "old", CancellationToken.None));
    }

    [Fact]
    public async Task GetNotebookById_DeniesNonOwnerOfPrivateNotebook()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var forbidden = await service.GetNotebookByIdAsync(PrivateNotebookId, OtherUserId, CancellationToken.None);
        Assert.False(forbidden.Succeeded);
        Assert.Equal(NotesFailureKind.Forbidden, forbidden.Error!.Kind);

        var allowed = await service.GetNotebookByIdAsync(PrivateNotebookId, OwnerId, CancellationToken.None);
        Assert.True(allowed.Succeeded);

        var missing = await service.GetNotebookByIdAsync(Guid.NewGuid(), OwnerId, CancellationToken.None);
        Assert.Equal(NotesFailureKind.NotFound, missing.Error!.Kind);
    }

    [Fact]
    public async Task GetNotebookItems_ExcludesArchivedByDefault_IncludesWhenRequested()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var active = await service.GetNotebookItemsAsync(PublicNotebookId, OwnerId, null, CancellationToken.None);
        Assert.True(active.Succeeded);
        Assert.Equal(2, active.Value!.Count);

        var withArchived = await service.GetNotebookItemsAsync(PublicNotebookId, OwnerId, null, CancellationToken.None, includeArchived: true);
        Assert.Equal(3, withArchived.Value!.Count);
    }

    [Fact]
    public async Task GetPublicNotebookItem_ReturnsItemBySlugAndPath()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var found = await service.GetPublicNotebookItemAsync("architecture-notes", "chapters/overview", CancellationToken.None);
        Assert.True(found.Succeeded);
        Assert.Equal("Overview", found.Value!.Title);

        var missing = await service.GetPublicNotebookItemAsync("architecture-notes", "chapters/missing", CancellationToken.None);
        Assert.Equal(NotesFailureKind.NotFound, missing.Error!.Kind);
    }

    [Fact]
    public async Task GetNotebookSummaryBySlug_ReturnsSummaryForAccessibleNotebook()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var result = await service.GetNotebookSummaryBySlugAsync("architecture-notes", OtherUserId, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("architecture-notes", result.Value!.Slug);
        Assert.Equal(2, result.Value.ItemCount);
        Assert.False(result.Value.CanEdit);
    }

    [Fact]
    public async Task GetNotebookItemsPage_AppliesParentTypeAndPaginationFilters()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateReadService(context);

        var result = await service.GetNotebookItemsPageAsync(
            PublicNotebookId,
            OwnerId,
            search: null,
            CancellationToken.None,
            includeArchived: false,
            parentId: FolderItemId,
            type: "page",
            offset: 0,
            limit: 1);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.TotalCount);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(PageItemId, item.Id);
        Assert.Equal("page", item.Type);
    }
}
