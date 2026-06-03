using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Tests;

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
            CreateItem(grandChildId, childId, "root/child/grand-child")
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
            CreateItem(movedId, null, "moved")
        };

        // Must not loop forever; treated as a cycle.
        Assert.True(NotebookItemTree.WouldCreateCycle(items, movedId, aId));
    }

    [Fact]
    public void GeneratePath_Uses_Deterministic_Suffix()
    {
        var itemId = Guid.NewGuid();
        var items = new[]
        {
            CreateItem(Guid.NewGuid(), null, "hello-world")
        };

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
            CreateItem(Guid.NewGuid(), null, "other")
        };

        var path = NotebookItemTree.GeneratePath(items, null, "Hello World", itemId);

        Assert.StartsWith("hello-world-", path);
        Assert.NotEqual("hello-world", path);
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
            CreateItem(grandChildId, childId, "old-root/child/grand-child")
        };

        NotebookItemTree.ApplyDescendantPathUpdate(items, rootId, "old-root", "new-root");

        Assert.Equal("new-root/child", items[1].Path);
        Assert.Equal("new-root/child/grand-child", items[2].Path);
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
            CreateItem(otherId, null, "other")
        };

        var descendantIds = NotebookItemTree.GetDescendantIds(items, rootId);

        Assert.Contains(childId, descendantIds);
        Assert.Contains(grandChildId, descendantIds);
        Assert.DoesNotContain(rootId, descendantIds);
        Assert.DoesNotContain(otherId, descendantIds);
    }

    private static NotebookItem CreateItem(Guid id, Guid? parentId, string path)
    {
        return new NotebookItem
        {
            Id = id,
            NotebookId = Guid.NewGuid(),
            ParentId = parentId,
            Type = NotebookItemType.Page,
            Title = path.Split('/')[^1],
            Slug = path.Split('/')[^1],
            Path = path
        };
    }
}
