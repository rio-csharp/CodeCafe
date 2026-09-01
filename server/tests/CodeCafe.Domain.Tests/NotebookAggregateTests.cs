using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.Events;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Tests;

public sealed class NotebookAggregateTests
{
    [Fact]
    public void AddItem_Generates_Path_Under_Parent()
    {
        var notebook = CreateNotebook();
        var folderId = Guid.NewGuid();
        Assert.Null(
            notebook.AddItem(folderId, NotebookItemType.Folder, "Guides", null, null, 1, DateTimeOffset.UtcNow)
        );

        var pageId = Guid.NewGuid();
        var violation = notebook.AddItem(
            pageId,
            NotebookItemType.Page,
            "Overview",
            null,
            folderId,
            2,
            DateTimeOffset.UtcNow
        );

        Assert.Null(violation);
        Assert.Equal("guides/overview", notebook.Items.Single(item => item.Id == pageId).Path.Value);
    }

    [Fact]
    public void AddItem_Returns_ParentNotFolder_When_Parent_Is_Page()
    {
        var notebook = CreateNotebook();
        var pageId = Guid.NewGuid();
        notebook.AddItem(pageId, NotebookItemType.Page, "Page", null, null, 1, DateTimeOffset.UtcNow);

        var violation = notebook.AddItem(
            Guid.NewGuid(),
            NotebookItemType.Page,
            "Child",
            null,
            pageId,
            2,
            DateTimeOffset.UtcNow
        );

        Assert.Equal(NotebookItemAddViolation.ParentNotFolder, violation);
    }

    [Fact]
    public void AddItem_Returns_ParentNotFound_When_Parent_Missing()
    {
        var notebook = CreateNotebook();

        var violation = notebook.AddItem(
            Guid.NewGuid(),
            NotebookItemType.Page,
            "Page",
            null,
            Guid.NewGuid(),
            1,
            DateTimeOffset.UtcNow
        );

        Assert.Equal(NotebookItemAddViolation.ParentNotFound, violation);
    }

    [Fact]
    public void AddItem_Uses_Custom_Slug_When_Provided()
    {
        var notebook = CreateNotebook();
        var folderId = Guid.NewGuid();
        notebook.AddItem(folderId, NotebookItemType.Folder, "Guides", null, null, 1, DateTimeOffset.UtcNow);
        var pageId = Guid.NewGuid();

        var violation = notebook.AddItem(
            pageId,
            NotebookItemType.Page,
            "Some Very Long Marketing Title",
            NotebookSlug.Create("getting-started"),
            folderId,
            2,
            DateTimeOffset.UtcNow
        );

        Assert.Null(violation);
        var page = notebook.Items.Single(item => item.Id == pageId);
        Assert.Equal("getting-started", page.Slug.Value);
        Assert.Equal("guides/getting-started", page.Path.Value);
        Assert.Equal("Some Very Long Marketing Title", page.Title);
    }

    [Fact]
    public void AddItem_Returns_SlugConflict_When_Custom_Slug_Taken()
    {
        var notebook = CreateNotebook();
        notebook.AddItem(
            Guid.NewGuid(),
            NotebookItemType.Page,
            "Overview",
            NotebookSlug.Create("overview"),
            null,
            1,
            DateTimeOffset.UtcNow
        );

        var violation = notebook.AddItem(
            Guid.NewGuid(),
            NotebookItemType.Page,
            "Another Page",
            NotebookSlug.Create("overview"),
            null,
            2,
            DateTimeOffset.UtcNow
        );

        Assert.Equal(NotebookItemAddViolation.SlugConflict, violation);
    }

    [Fact]
    public void RenameItem_Changes_Title_But_Keeps_Path()
    {
        var notebook = CreateNotebook();
        var pageId = Guid.NewGuid();
        notebook.AddItem(pageId, NotebookItemType.Page, "Old Title", null, null, 1, DateTimeOffset.UtcNow);

        var violation = notebook.RenameItem(pageId, "New Title");

        Assert.Null(violation);
        var page = notebook.Items.Single(item => item.Id == pageId);
        Assert.Equal("New Title", page.Title);
        Assert.Equal("old-title", page.Slug.Value);
        Assert.Equal("old-title", page.Path.Value);
    }

    [Fact]
    public void RenameItem_Returns_NotFound_For_Missing_Item()
    {
        var notebook = CreateNotebook();

        Assert.Equal(NotebookItemRenameViolation.NotFound, notebook.RenameItem(Guid.NewGuid(), "X"));
    }

    [Fact]
    public void ChangeSlug_Updates_Slug_Explicitly()
    {
        var notebook = CreateNotebook();

        notebook.ChangeSlug(NotebookSlug.Create("new-address"));

        Assert.Equal("new-address", notebook.Slug.Value);
    }

    [Fact]
    public void ArchiveItem_Cascades_To_Descendants_And_Raises_Event()
    {
        var notebook = CreateNotebook();
        var folderId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        notebook.AddItem(folderId, NotebookItemType.Folder, "Guides", null, null, 1, now);
        notebook.AddItem(pageId, NotebookItemType.Page, "Overview", null, folderId, 2, now);

        var violation = notebook.ArchiveItem(folderId, actorId, now);

        Assert.Null(violation);
        Assert.True(notebook.Items.Single(item => item.Id == folderId).IsArchived);
        Assert.True(notebook.Items.Single(item => item.Id == pageId).IsArchived);
        var domainEvent = Assert.IsType<NotebookItemArchivedDomainEvent>(
            Assert.Single(notebook.DomainEvents)
        );
        Assert.Equal(notebook.Id, domainEvent.NotebookId);
        Assert.Equal(folderId, domainEvent.ItemId);
        Assert.Equal(actorId, domainEvent.ArchivedByUserId);
    }

    [Fact]
    public void ArchiveItem_Returns_Violations_For_Missing_Or_Archived_Item()
    {
        var notebook = CreateNotebook();
        var now = DateTimeOffset.UtcNow;

        Assert.Equal(
            NotebookItemArchiveViolation.NotFound,
            notebook.ArchiveItem(Guid.NewGuid(), Guid.NewGuid(), now)
        );

        var pageId = Guid.NewGuid();
        notebook.AddItem(pageId, NotebookItemType.Page, "Page", null, null, 1, now);
        notebook.ArchiveItem(pageId, Guid.NewGuid(), now);

        Assert.Equal(
            NotebookItemArchiveViolation.AlreadyArchived,
            notebook.ArchiveItem(pageId, Guid.NewGuid(), now)
        );
    }

    [Fact]
    public void RestoreItem_Restores_Subtree_And_Raises_Event()
    {
        var notebook = CreateNotebook();
        var folderId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        notebook.AddItem(folderId, NotebookItemType.Folder, "Guides", null, null, 1, now);
        notebook.AddItem(pageId, NotebookItemType.Page, "Overview", null, folderId, 2, now);
        notebook.ArchiveItem(folderId, Guid.NewGuid(), now);
        notebook.ClearDomainEvents();

        var violation = notebook.RestoreItem(folderId, now);

        Assert.Null(violation);
        Assert.False(notebook.Items.Single(item => item.Id == folderId).IsArchived);
        Assert.False(notebook.Items.Single(item => item.Id == pageId).IsArchived);
        Assert.IsType<NotebookItemRestoredDomainEvent>(Assert.Single(notebook.DomainEvents));
    }

    [Fact]
    public void RestoreItem_Returns_Violations_For_Missing_Or_Active_Item()
    {
        var notebook = CreateNotebook();
        var now = DateTimeOffset.UtcNow;

        Assert.Equal(
            NotebookItemRestoreViolation.NotFound,
            notebook.RestoreItem(Guid.NewGuid(), now)
        );

        var pageId = Guid.NewGuid();
        notebook.AddItem(pageId, NotebookItemType.Page, "Page", null, null, 1, now);

        Assert.Equal(NotebookItemRestoreViolation.NotArchived, notebook.RestoreItem(pageId, now));
    }

    [Fact]
    public void AddItem_Custom_Slug_Exceeding_Budget_Returns_NoRoomForChild()
    {
        var notebook = CreateNotebook();
        var oversized = NotebookSlug.Create(new string('a', 50));
        var deepFolderId = Guid.NewGuid();
        var deepPath = new string('f', 970) + "/leaf";
        notebook.AddItem(deepFolderId, NotebookItemType.Folder, "Deep", null, null, 1, DateTimeOffset.UtcNow);
        var folder = notebook.Items.Single(item => item.Id == deepFolderId);
        folder.UpdateStructure(null, "Deep", deepPath);

        var violation = notebook.AddItem(
            Guid.NewGuid(),
            NotebookItemType.Page,
            "Page",
            oversized,
            deepFolderId,
            2,
            DateTimeOffset.UtcNow
        );

        Assert.Equal(NotebookItemAddViolation.NoRoomForChild, violation);
    }

    [Fact]
    public void ApplyVisibility_Raises_Event_With_Old_And_New_Value()
    {
        var notebook = CreateNotebook();
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        notebook.ApplyVisibility(NotebookVisibility.Public, now);

        var domainEvent = Assert.IsType<NotebookVisibilityChangedDomainEvent>(
            Assert.Single(notebook.DomainEvents)
        );
        Assert.Equal(NotebookVisibility.Private, domainEvent.OldVisibility);
        Assert.Equal(NotebookVisibility.Public, domainEvent.NewVisibility);
        Assert.Equal(now, domainEvent.OccurredAtUtc);
    }

    [Fact]
    public void ApplyVisibility_Same_Value_Is_A_NoOp_Without_Event()
    {
        var notebook = CreateNotebook();

        notebook.ApplyVisibility(NotebookVisibility.Private, DateTimeOffset.UtcNow);

        Assert.Empty(notebook.DomainEvents);
    }

    private static Notebook CreateNotebook()
    {
        return Notebook.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Architecture Notes",
            NotebookSlug.Create("architecture-notes"),
            null,
            NotebookVisibility.Private,
            DateTimeOffset.UtcNow
        );
    }
}
