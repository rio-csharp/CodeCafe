using CodeCafe.Application.Notebooks;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class NotebookSlugTests
{
    [Fact]
    public void WithSuffix_AppendsToShortBases()
    {
        Assert.Equal("my-note-4821", NotebookSlug.WithSuffix("my-note", "4821"));
    }

    [Fact]
    public void WithSuffix_TruncatesTheBase_SoTheResultStillFitsTheStoredColumn()
    {
        var baseSlug = new string('a', Notebook.MaxSlugLength);

        var slug = NotebookSlug.WithSuffix(baseSlug, "1234");

        Assert.Equal(Notebook.MaxSlugLength, slug.Length);
        Assert.EndsWith("-1234", slug);
        Assert.True(NotebookSlug.IsValid(slug));
    }
}
