using CodeCafe.Application.Pages.SearchAllPages;

namespace CodeCafe.Application.Tests.Pages;

public sealed class SearchAllPagesQueryValidatorTests
{
    private readonly SearchAllPagesQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        var result = _validator.Validate(new SearchAllPagesQuery("rust", null, null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Query_Fails(string query)
    {
        var result = _validator.Validate(new SearchAllPagesQuery(query, null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SearchAllPagesQuery.Query));
    }

    [Fact]
    public void Oversized_Query_Fails()
    {
        var query = new string('a', SearchAllPagesQueryValidator.MaxQueryLength + 1);

        var result = _validator.Validate(new SearchAllPagesQuery(query, null, null));

        Assert.False(result.IsValid);
    }
}
