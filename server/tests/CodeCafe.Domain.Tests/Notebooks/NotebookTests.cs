using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Domain.Tests.Notebooks;

public sealed class NotebookTests
{
    [Fact]
    public void SetTags_RefreshesUpdatedAtUtc_TagsAreContent()
    {
        var notebook = CreateNotebook();
        var before = notebook.UpdatedAtUtc;

        notebook.SetTags(["work"]);

        Assert.True(notebook.UpdatedAtUtc > before);
    }

    [Fact]
    public void SetAccessCodeHash_DoesNotRefreshUpdatedAtUtc_AccessControlIsNotContent()
    {
        var notebook = CreateNotebook();
        var before = notebook.UpdatedAtUtc;

        notebook.SetAccessCodeHash("hash");

        Assert.Equal(before, notebook.UpdatedAtUtc);
    }

    [Fact]
    public void Share_DoesNotRefreshUpdatedAtUtc_AccessControlIsNotContent()
    {
        var notebook = CreateNotebook();
        var before = notebook.UpdatedAtUtc;

        notebook.Share(Guid.NewGuid(), CollaboratorRole.Viewer);

        Assert.Equal(before, notebook.UpdatedAtUtc);
    }

    [Fact]
    public void SetTags_BeyondMaxCount_Throws()
    {
        var notebook = CreateNotebook();
        var tags = Enumerable.Range(1, Notebook.MaxTagCount + 1).Select(i => $"tag-{i}");

        Assert.Throws<ArgumentException>(() => notebook.SetTags(tags));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetTags_BlankTag_Throws(string tag)
    {
        var notebook = CreateNotebook();

        Assert.Throws<ArgumentException>(() => notebook.SetTags([tag]));
    }

    [Fact]
    public void SetTags_OversizedTag_Throws()
    {
        var notebook = CreateNotebook();

        Assert.Throws<ArgumentException>(() => notebook.SetTags([new string('a', Notebook.MaxTagLength + 1)]));
    }

    private static Notebook CreateNotebook()
        => Notebook.Create(Guid.NewGuid(), "My Notes", null, "my-notes", NotebookVisibility.Private);
}
