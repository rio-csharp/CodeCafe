using CodeCafe.Domain.Notes;

namespace CodeCafe.Domain.Tests;

public sealed class NotebookItemTreeTests
{
    [Fact]
    public void WouldCreateCycle_Detects_Move_Into_Descendant()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandChildId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(rootId, null, "root"),
            CreateItem(childId, rootId, "root/child"),
            CreateItem(grandChildId, childId, "root/child/grand-child"),
        };

        Assert.True(NotebookItemTree.WouldCreateCycle(items, childId, grandChildId));
    }

    [Fact]
    public void WouldCreateCycle_Terminates_On_PreExistingCycle_Not_Involving_Item()
    {
        // a -> b -> a is a pre-existing cycle that does not involve the moved item.
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var movedId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(aId, bId, "a"),
            CreateItem(bId, aId, "b"),
            CreateItem(movedId, null, "moved"),
        };

        // Must not loop forever; treated as a cycle.
        Assert.True(NotebookItemTree.WouldCreateCycle(items, movedId, aId));
    }

    [Fact]
    public void WouldCreateCycle_Detects_Cycle_From_Pending_Reparent_Overrides()
    {
        // Reorder batch that swaps two folders: a -> b and b -> a.
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var items = new[] { CreateItem(aId, null, "a"), CreateItem(bId, null, "b") };
        var overrides = new List<(Guid ItemId, Guid? ParentId)> { (aId, bId), (bId, aId) };

        Assert.True(NotebookItemTree.WouldCreateCycle(items, aId, bId, overrides));
        Assert.True(NotebookItemTree.WouldCreateCycle(items, bId, aId, overrides));
    }

    [Fact]
    public void WouldCreateCycle_Allows_Reparent_When_Overrides_Are_Acyclic()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(aId, null, "a"),
            CreateItem(bId, aId, "a/b"),
            CreateItem(rootId, null, "root"),
        };
        var overrides = new List<(Guid ItemId, Guid? ParentId)> { (bId, rootId) };

        Assert.False(NotebookItemTree.WouldCreateCycle(items, bId, rootId, overrides));
    }

    [Fact]
    public void GeneratePath_Uses_Deterministic_Suffix()
    {
        var itemId = Guid.NewGuid();
        var items = new[] { CreateItem(Guid.NewGuid(), null, "hello-world") };

        var path = NotebookItemTree.GeneratePath(items, null, "Hello World", itemId);

        Assert.Equal("hello-world-1", path);
    }

    [Fact]
    public void GeneratePath_Adds_Suffix_When_Sibling_Path_Already_Exists()
    {
        var itemId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(Guid.NewGuid(), null, "hello-world"),
            CreateItem(Guid.NewGuid(), null, "other"),
        };

        var path = NotebookItemTree.GeneratePath(items, null, "Hello World", itemId);

        Assert.StartsWith("hello-world-", path);
        Assert.NotEqual("hello-world", path);
    }

    [Fact]
    public void GeneratePath_Nests_Under_Parent_Path()
    {
        var itemId = Guid.NewGuid();
        var items = Array.Empty<NotebookItem>();

        var path = NotebookItemTree.GeneratePath(items, "folder/sub", "My Page", itemId);

        Assert.Equal("folder/sub/my-page", path);
    }

    [Fact]
    public void GeneratePath_Ignores_Self_When_Title_Unchanged()
    {
        var itemId = Guid.NewGuid();
        var items = new[] { CreateItem(itemId, null, "hello-world", "Hello World") };

        var path = NotebookItemTree.GeneratePath(items, null, "Hello World", itemId);

        Assert.Equal("hello-world", path);
    }

    [Fact]
    public void GeneratePath_Falls_Back_To_Unique_Slug_After_Ten_Conflicts()
    {
        var itemId = Guid.NewGuid();
        var items = Enumerable
            .Range(0, 10)
            .Select(attempt =>
                CreateItem(
                    Guid.NewGuid(),
                    null,
                    NotebookSlugGenerator.WithSuffix("hello-world", attempt)
                )
            )
            .ToArray();

        var path = NotebookItemTree.GeneratePath(items, null, "Hello World", itemId);

        Assert.StartsWith("hello-world-", path);
        Assert.DoesNotContain(items, item => item.Path == path);
    }

    [Fact]
    public void ValidateParentCandidate_Allows_Root_Placement()
    {
        Assert.Null(NotebookItemTree.ValidateParentCandidate(parent: null, parentId: null));
    }

    [Fact]
    public void ValidateParentCandidate_Returns_NotFound_When_Parent_Missing()
    {
        Assert.Equal(
            NotebookItemParentViolation.NotFound,
            NotebookItemTree.ValidateParentCandidate(parent: null, parentId: Guid.NewGuid())
        );
    }

    [Fact]
    public void ValidateParentCandidate_Returns_NotFolder_When_Parent_Is_Page()
    {
        var parent = CreateItem(Guid.NewGuid(), null, "page", type: NotebookItemType.Page);

        Assert.Equal(
            NotebookItemParentViolation.NotFolder,
            NotebookItemTree.ValidateParentCandidate(parent, parent.Id)
        );
    }

    [Fact]
    public void ValidateParentCandidate_Allows_Folder_Parent()
    {
        var parent = CreateItem(Guid.NewGuid(), null, "folder", type: NotebookItemType.Folder);

        Assert.Null(NotebookItemTree.ValidateParentCandidate(parent, parent.Id));
    }

    [Fact]
    public void ValidateRestore_Returns_NotArchived_When_Item_Is_Active()
    {
        var item = CreateItem(Guid.NewGuid(), null, "page");

        Assert.Equal(
            NotebookItemRestoreViolation.NotArchived,
            NotebookItemTree.ValidateRestore([item], item)
        );
    }

    [Fact]
    public void ValidateRestore_Returns_ParentNotFound_When_Parent_Missing()
    {
        var item = CreateItem(Guid.NewGuid(), Guid.NewGuid(), "folder/page", archived: true);

        Assert.Equal(
            NotebookItemRestoreViolation.ParentNotFound,
            NotebookItemTree.ValidateRestore([item], item)
        );
    }

    [Fact]
    public void ValidateRestore_Returns_ParentArchived_When_Parent_Archived_Outside_Subtree()
    {
        var parentId = Guid.NewGuid();
        var item = CreateItem(Guid.NewGuid(), parentId, "folder/page", archived: true);
        var parent = CreateItem(parentId, null, "folder", archived: true);
        var items = new[] { item, parent };

        Assert.Equal(
            NotebookItemRestoreViolation.ParentArchived,
            NotebookItemTree.ValidateRestore(items, item)
        );
    }

    [Fact]
    public void ValidateRestore_Allows_When_Archived_Parent_Is_Inside_Subtree()
    {
        // Restoring the folder itself restores the archived page inside it,
        // so the archived parent chain no longer blocks the restore.
        var folderId = Guid.NewGuid();
        var folder = CreateItem(folderId, null, "folder", archived: true);
        var child = CreateItem(Guid.NewGuid(), folderId, "folder/page", archived: true);
        var items = new[] { folder, child };

        Assert.Null(NotebookItemTree.ValidateRestore(items, folder));
    }

    [Fact]
    public void ValidateRestore_Allows_Archived_Item_With_Active_Parent()
    {
        var parentId = Guid.NewGuid();
        var item = CreateItem(Guid.NewGuid(), parentId, "folder/page", archived: true);
        var parent = CreateItem(parentId, null, "folder");
        var items = new[] { item, parent };

        Assert.Null(NotebookItemTree.ValidateRestore(items, item));
    }

    [Fact]
    public void ApplyDescendantPathUpdate_Rewrites_Children()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandChildId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(rootId, null, "old-root"),
            CreateItem(childId, rootId, "old-root/child"),
            CreateItem(grandChildId, childId, "old-root/child/grand-child"),
        };

        NotebookItemTree.ApplyDescendantPathUpdate(items, rootId, "old-root", "new-root");

        Assert.Equal("new-root/child", items[1].Path);
        Assert.Equal("new-root/child/grand-child", items[2].Path);
    }

    [Fact]
    public void ApplyDescendantPathUpdate_Rewrites_Subtree_On_Folder_Move()
    {
        var rootId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(rootId, null, "folder"),
            CreateItem(targetId, null, "archive"),
            CreateItem(childId, rootId, "folder/page"),
        };

        NotebookItemTree.ApplyDescendantPathUpdate(items, rootId, "folder", "archive/folder");

        Assert.Equal("archive/folder/page", items[2].Path);
        Assert.Equal("archive", items[1].Path);
    }

    [Fact]
    public void GetDescendantIds_Returns_Full_Subtree()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandChildId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(rootId, null, "root"),
            CreateItem(childId, rootId, "root/child"),
            CreateItem(grandChildId, childId, "root/child/grand-child"),
            CreateItem(otherId, null, "other"),
        };

        var descendantIds = NotebookItemTree.GetDescendantIds(items, rootId);

        Assert.Contains(childId, descendantIds);
        Assert.Contains(grandChildId, descendantIds);
        Assert.DoesNotContain(rootId, descendantIds);
        Assert.DoesNotContain(otherId, descendantIds);
    }

    private static NotebookItem CreateItem(
        Guid id,
        Guid? parentId,
        string path,
        string? title = null,
        NotebookItemType type = NotebookItemType.Page,
        bool archived = false
    )
    {
        return new NotebookItem
        {
            Id = id,
            NotebookId = Guid.NewGuid(),
            ParentId = parentId,
            Type = type,
            Title = title ?? path.Split('/')[^1],
            Slug = path.Split('/')[^1],
            Path = path,
            IsArchived = archived,
        };
    }
}
