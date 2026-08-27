using CodeCafe.Domain.Notes;

namespace CodeCafe.Domain.Tests;

public sealed class NotebookItemTests
{
    [Fact]
    public void UpdateStructure_Refreshes_Path_And_Slug()
    {
        var parentId = Guid.NewGuid();
        var item = CreateItem();

        item.UpdateStructure(parentId, "Updated", "folder/updated", 7);

        Assert.Equal(parentId, item.ParentId);
        Assert.Equal("Updated", item.Title);
        Assert.Equal("folder/updated", item.Path);
        Assert.Equal("updated", item.Slug);
        Assert.Equal(7, item.SortOrder);
    }

    [Fact]
    public void SetPageContent_Stores_PagePayload()
    {
        var item = CreateItem();

        item.SetPageContent("tiptap_json", "{\"type\":\"doc\"}", "hello");

        Assert.Equal("tiptap_json", item.ContentFormat);
        Assert.Equal("{\"type\":\"doc\"}", item.ContentJson);
        Assert.Equal("hello", item.PlainTextContent);
    }

    [Fact]
    public void Archive_And_Restore_Toggle_Archive_State()
    {
        var item = CreateItem();
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();

        item.Archive(now, userId);
        Assert.True(item.IsArchived);
        Assert.Equal(now, item.ArchivedAtUtc);
        Assert.Equal(userId, item.ArchivedByUserId);

        item.Restore();
        Assert.False(item.IsArchived);
        Assert.Null(item.ArchivedAtUtc);
        Assert.Null(item.ArchivedByUserId);
    }

    private static NotebookItem CreateItem()
    {
        return new NotebookItem
        {
            Id = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            Type = NotebookItemType.Page,
            Title = "Original",
            Slug = "original",
            Path = "original",
        };
    }
}
