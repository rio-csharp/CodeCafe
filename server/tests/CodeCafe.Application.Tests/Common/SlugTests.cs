using CodeCafe.Application.Common;

namespace CodeCafe.Application.Tests.Common;

public sealed class SlugTests
{
    [Fact]
    public void WithSuffix_AppendsToShortBases()
    {
        Assert.Equal("my-note-4821", Slug.WithSuffix("my-note", "4821", maxLength: 64));
    }

    [Fact]
    public void WithSuffix_TruncatesTheBase_SoTheResultStillFitsTheStoredColumn()
    {
        var baseSlug = new string('a', 64);

        var slug = Slug.WithSuffix(baseSlug, "1234", maxLength: 64);

        Assert.Equal(64, slug.Length);
        Assert.EndsWith("-1234", slug);
        Assert.True(Slug.IsValid(slug, 64));
    }
}
