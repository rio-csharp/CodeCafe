using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.Services;
using CodeCafe.Domain.Notes.ValueObjects;

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
        Assert.Equal("folder/updated", item.Path.Value);
        Assert.Equal("updated", item.Slug.Value);
        Assert.Equal(7, item.SortOrder);
    }

    [Fact]
    public void SetPageContent_Stores_Payload_And_Derives_PlainText()
    {
        var item = CreateItem();

        item.SetPageContent(
            """
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"hello "},{"type":"text","text":"world","marks":[{"type":"bold"}]}]}]}
            """
        );

        Assert.Equal("hello world", item.PlainTextContent);
    }

    [Fact]
    public void SetPageContent_Derives_Placeholders_And_Separators()
    {
        var item = CreateItem();

        item.SetPageContent(
            """
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"first"}]},{"type":"image","attrs":{"src":"https://example.com/a.png"}},{"type":"paragraph","content":[{"type":"text","text":"last"}]}]}
            """
        );

        Assert.Equal("first" + Environment.NewLine + "[Image]" + Environment.NewLine + "last", item.PlainTextContent);
    }

    [Fact]
    public void SetPageContent_Null_Or_Invalid_Json_Yields_Null_PlainText()
    {
        var item = CreateItem();

        item.SetPageContent(null);
        Assert.Null(item.PlainTextContent);

        item.SetPageContent("not json");
        Assert.Null(item.PlainTextContent);
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
        return NotebookItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            NotebookItemType.Page,
            "Original",
            NotebookSlug.Create("original"),
            NotebookPath.Create("original"),
            0,
            DateTimeOffset.UtcNow
        );
    }
}
