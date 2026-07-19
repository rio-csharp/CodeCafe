using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Domain.Notes;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Tests;

public sealed class NotebookItemMutationServiceTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid NotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FolderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PageId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly JsonElement Undefined = default;

    private static NotesDbHarness CreateSeededHarness()
    {
        var harness = new NotesDbHarness();
        using var seed = harness.CreateContext();

        seed.AddUser(OwnerId, "Yao");
        seed.AddUser(OtherUserId, "Mei");
        seed.AddNotebook(NotebookId, OwnerId, "Architecture Notes", "architecture-notes", NotebookVisibility.Public, true);
        seed.AddItem(FolderId, NotebookId, NotebookItemType.Folder, "Chapters", "chapters", 0);
        seed.AddItem(PageId, NotebookId, NotebookItemType.Page, "Overview", "chapters/overview", 1, parentId: FolderId, plainTextContent: "overview");
        seed.SaveChanges();

        return harness;
    }

    [Fact]
    public async Task CreateItem_DeniesNonOwner_AndReportsMissingNotebook()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateMutationService(context);

        var forbidden = await service.CreateNotebookItemAsync(NotebookId, OtherUserId, null, "page", "New", 0, null, CancellationToken.None);
        Assert.Equal(NotesFailureKind.Forbidden, forbidden.Error!.Kind);

        var missing = await service.CreateNotebookItemAsync(Guid.NewGuid(), OwnerId, null, "page", "New", 0, null, CancellationToken.None);
        Assert.Equal(NotesFailureKind.NotFound, missing.Error!.Kind);
    }

    [Fact]
    public async Task CreateItem_RejectsInvalidType_AndNonFolderParent()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateMutationService(context);

        var badType = await service.CreateNotebookItemAsync(NotebookId, OwnerId, null, "section", "X", 0, null, CancellationToken.None);
        Assert.Equal("invalid_item_type", badType.Error!.Code);

        var pageParent = await service.CreateNotebookItemAsync(NotebookId, OwnerId, PageId, "page", "Child", 0, null, CancellationToken.None);
        Assert.Equal("invalid_parent", pageParent.Error!.Code);
    }

    [Fact]
    public async Task CreateItem_PersistsPageUnderFolder_WithGeneratedPath()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateMutationService(context);

        var created = await service.CreateNotebookItemAsync(NotebookId, OwnerId, FolderId, "page", "Deep Dive", 2, null, CancellationToken.None);

        Assert.True(created.Succeeded);
        Assert.Equal("chapters/deep-dive", created.Value!.Path);
        Assert.Equal(FolderId, created.Value.ParentId);
    }

    [Fact]
    public async Task UpdateItem_Renames_RegeneratesPath_AndRepathsDescendants()
    {
        using var harness = CreateSeededHarness();
        await using (var arrange = harness.CreateContext())
        {
            // page nested below the folder so renaming the folder must cascade.
            arrange.AddItem(Guid.NewGuid(), NotebookId, NotebookItemType.Page, "Nested", "chapters/nested", 5, parentId: FolderId);
            await arrange.SaveChangesAsync();
        }

        await using (var act = harness.CreateContext())
        {
            var service = harness.CreateMutationService(act);
            var updated = await service.UpdateNotebookItemAsync(
                NotebookId, FolderId, OwnerId, "Sections", Undefined, null, Undefined, CancellationToken.None);

            Assert.True(updated.Succeeded);
            Assert.Equal("sections", updated.Value!.Path);
        }

        await using var assertContext = harness.CreateContext();
        var nested = assertContext.NotebookItems.Single(item => item.Title == "Nested");
        Assert.Equal("sections/nested", nested.Path);
    }

    [Fact]
    public async Task UpdateItem_DetectsStaleExpectedTimestamp()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateMutationService(context);

        var conflict = await service.UpdateNotebookItemAsync(
            NotebookId, PageId, OwnerId, "Renamed", Undefined, null, Undefined, CancellationToken.None,
            expectedUpdatedAtUtc: DateTimeOffset.Parse("2000-01-01T00:00:00+00:00"));

        Assert.Equal(NotesFailureKind.Conflict, conflict.Error!.Kind);
        Assert.Equal("content_conflict", conflict.Error.Code);
    }

    [Fact]
    public async Task UpdateItem_RejectsCyclicReparent()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateMutationService(context);

        // Move the folder under its own child page is invalid; build a folder-under-folder cycle instead.
        var childFolderId = Guid.NewGuid();
        context.AddItem(childFolderId, NotebookId, NotebookItemType.Folder, "Child", "chapters/child", 3, parentId: FolderId);
        await context.SaveChangesAsync();

        var childParent = JsonSerializer.SerializeToElement(childFolderId.ToString());
        var cyclic = await service.UpdateNotebookItemAsync(
            NotebookId, FolderId, OwnerId, "Chapters", childParent, null, Undefined, CancellationToken.None);

        Assert.Equal("invalid_parent", cyclic.Error!.Code);
    }

    [Fact]
    public async Task ReorderItems_UpdatesSortOrder_AndParenting()
    {
        using var harness = CreateSeededHarness();
        await using var context = harness.CreateContext();
        var service = harness.CreateMutationService(context);

        var reorder = await service.ReorderNotebookItemsAsync(
            NotebookId,
            OwnerId,
            [new ReorderNotebookItemModel(PageId, null, 9)],
            CancellationToken.None);

        Assert.True(reorder.Succeeded);
        var moved = reorder.Value!.Single(item => item.Id == PageId);
        Assert.Null(moved.ParentId);
        Assert.Equal(9, moved.SortOrder);
        Assert.Equal("overview", moved.Path);
    }

    [Fact]
    public async Task Archive_Then_Restore_CascadesAcrossSubtree()
    {
        using var harness = CreateSeededHarness();

        await using (var act = harness.CreateContext())
        {
            var service = harness.CreateMutationService(act);
            var archived = await service.ArchiveNotebookItemAsync(NotebookId, FolderId, OwnerId, CancellationToken.None);
            Assert.True(archived.Succeeded);
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.True(verify.NotebookItems.Single(item => item.Id == PageId).IsArchived);
        }

        await using (var act = harness.CreateContext())
        {
            var service = harness.CreateMutationService(act);
            var restored = await service.RestoreNotebookItemAsync(NotebookId, FolderId, OwnerId, CancellationToken.None);
            Assert.True(restored.Succeeded);
        }

        await using var assertContext = harness.CreateContext();
        Assert.False(assertContext.NotebookItems.Single(item => item.Id == PageId).IsArchived);
    }

    [Fact]
    public async Task Restore_RejectsWhenParentStillArchived()
    {
        using var harness = CreateSeededHarness();

        await using (var act = harness.CreateContext())
        {
            var service = harness.CreateMutationService(act);
            await service.ArchiveNotebookItemAsync(NotebookId, FolderId, OwnerId, CancellationToken.None);
        }

        await using var context = harness.CreateContext();
        var restoreService = harness.CreateMutationService(context);
        var restoreChild = await restoreService.RestoreNotebookItemAsync(NotebookId, PageId, OwnerId, CancellationToken.None);

        Assert.Equal("parent_archived", restoreChild.Error!.Code);
    }

    [Fact]
    public async Task Delete_RequiresArchivedFirst_ThenRemovesSubtree()
    {
        using var harness = CreateSeededHarness();

        await using (var act = harness.CreateContext())
        {
            var service = harness.CreateMutationService(act);
            var notArchived = await service.DeleteNotebookItemAsync(NotebookId, FolderId, OwnerId, CancellationToken.None);
            Assert.Equal("notebook_item_not_archived", notArchived.Error!.Code);

            await service.ArchiveNotebookItemAsync(NotebookId, FolderId, OwnerId, CancellationToken.None);
            var deleted = await service.DeleteNotebookItemAsync(NotebookId, FolderId, OwnerId, CancellationToken.None);
            Assert.True(deleted.Succeeded);
        }

        await using var assertContext = harness.CreateContext();
        Assert.Empty(assertContext.NotebookItems.Where(item => item.NotebookId == NotebookId));
    }
}
