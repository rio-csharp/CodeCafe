using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.Services;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Tests;

public sealed class NotebookTests
{
    [Fact]
    public void ApplyVisibility_Sets_PublishedAt_When_First_Published()
    {
        var notebook = CreateNotebook();
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

        notebook.ApplyVisibility(NotebookVisibility.Public, now);

        Assert.Equal(NotebookVisibility.Public, notebook.Visibility);
        Assert.Equal(now, notebook.PublishedAtUtc);
    }

    [Fact]
    public void ApplyVisibility_Clears_PublishedAt_When_Made_Private()
    {
        var notebook = CreateNotebook();
        notebook.ApplyVisibility(
            NotebookVisibility.Public,
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
        );

        notebook.ApplyVisibility(
            NotebookVisibility.Private,
            new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero)
        );

        Assert.Equal(NotebookVisibility.Private, notebook.Visibility);
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

    private static Notebook CreateNotebook(
        NotebookVisibility visibility = NotebookVisibility.Private
    )
    {
        return Notebook.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Original",
            NotebookSlug.Create("original"),
            null,
            visibility,
            DateTimeOffset.UtcNow
        );
    }


    [Fact]
    public void SetAccessCode_On_Unlisted_Sets_Hash()
    {
        var notebook = CreateNotebook(NotebookVisibility.Unlisted);

        var violation = notebook.SetAccessCode("hash:secret");

        Assert.Null(violation);
        Assert.Equal("hash:secret", notebook.AccessCodeHash);
    }

    [Fact]
    public void SetAccessCode_On_Non_Unlisted_Returns_Violation()
    {
        var notebook = CreateNotebook(NotebookVisibility.Private);

        var violation = notebook.SetAccessCode("hash:secret");

        Assert.Equal(NotebookAccessCodeViolation.NotUnlisted, violation);
        Assert.Null(notebook.AccessCodeHash);
    }

    [Fact]
    public void ApplyVisibility_Away_From_Unlisted_Clears_AccessCode()
    {
        var notebook = CreateNotebook(NotebookVisibility.Unlisted);
        notebook.SetAccessCode("hash:secret");

        notebook.ApplyVisibility(NotebookVisibility.Public, DateTimeOffset.UtcNow);

        Assert.Null(notebook.AccessCodeHash);
    }

    [Fact]
    public void ApplyVisibility_Unlisted_To_Unlisted_Is_NoOp_Keeping_Code()
    {
        var notebook = CreateNotebook(NotebookVisibility.Unlisted);
        notebook.SetAccessCode("hash:secret");

        notebook.ApplyVisibility(NotebookVisibility.Unlisted, DateTimeOffset.UtcNow);

        Assert.Equal("hash:secret", notebook.AccessCodeHash);
    }
}
