using CodeCafe.Modules.Notes.Domain.Notes;

namespace CodeCafe.Domain.Tests;

public sealed class NotebookTests
{
    [Fact]
    public void ApplyVisibility_Sets_PublishedAt_When_First_Published()
    {
        var notebook = CreateNotebook();
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        notebook.ApplyVisibility(NotebookVisibility.Public, now);

        Assert.True(notebook.IsPublished);
        Assert.Equal(now, notebook.PublishedAtUtc);
    }

    [Fact]
    public void ApplyVisibility_Clears_PublishedAt_When_Made_Private()
    {
        var notebook = CreateNotebook();
        notebook.ApplyVisibility(NotebookVisibility.Public, new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));

        notebook.ApplyVisibility(NotebookVisibility.Private, new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));

        Assert.False(notebook.IsPublished);
        Assert.Null(notebook.PublishedAtUtc);
    }

    [Fact]
    public void Rename_And_SetDescription_Update_Core_Fields()
    {
        var notebook = CreateNotebook();

        notebook.Rename("Updated title");
        notebook.SetDescription("Updated description");

        Assert.Equal("Updated title", notebook.Title);
        Assert.Equal("Updated description", notebook.Description);
    }

    private static Notebook CreateNotebook()
    {
        return new Notebook
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Title = "Original",
            Slug = "original"
        };
    }
}
