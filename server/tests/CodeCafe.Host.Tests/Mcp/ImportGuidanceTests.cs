using CodeCafe.Host.Hosting;
using CodeCafe.Host.Mcp;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.Host.Tests.Mcp;

public sealed class ImportGuidanceTests
{
    private static readonly McpOptions Options = new() { MaxInlineContentBytes = 1024 };

    private static HttpContext Request()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("api.codes.cafe");
        return context;
    }

    [Fact]
    public void Recipe_AdvertisesTheOriginTheCallerReached()
    {
        var recipe = ImportGuidance.Recipe(Request(), Options);

        Assert.Equal("https://api.codes.cafe/api/notebooks/import/upload", recipe.Notebook.Url);
        Assert.Equal(
            "https://api.codes.cafe/api/notebooks/{notebookIdOrSlug}/pages/import/upload",
            recipe.Page.Url
        );
        Assert.Equal(1024, recipe.InlineContentLimitBytes);
        Assert.Equal(RequestLimits.ImportBytes, recipe.MaxUploadBytes);
        Assert.Contains("-F file=@", recipe.Notebook.Example, StringComparison.Ordinal);
        Assert.Contains("Bearer", recipe.Authorization, StringComparison.Ordinal);
    }

    [Fact]
    public void Recipe_StillDescribesTheShapeWithoutARequest()
    {
        // Reached only by a caller outside a request; the example must stay readable rather
        // than render an empty origin.
        var recipe = ImportGuidance.Recipe(null, Options);

        Assert.Contains("api/notebooks/import/upload", recipe.Notebook.Url, StringComparison.Ordinal);
        Assert.DoesNotContain(":///", recipe.Notebook.Url, StringComparison.Ordinal);
    }

    [Fact]
    public void TooLarge_AcceptsContentExactlyAtTheLimit()
    {
        var recipe = ImportGuidance.Recipe(Request(), Options);

        Assert.Null(ImportGuidance.TooLarge(new string('a', 1024), recipe));
    }

    [Fact]
    public void TooLarge_RefusesOversizedContentAndHandsBackTheRecipe()
    {
        var recipe = ImportGuidance.Recipe(Request(), Options);

        var result = ImportGuidance.TooLarge(new string('a', 1025), recipe);

        Assert.NotNull(result);
        Assert.True(result.IsError);
        var structured = result.StructuredContent!.Value;
        Assert.Equal("content_too_large_for_tool_argument", structured.GetProperty("code").GetString());
        Assert.Equal("Validation", structured.GetProperty("kind").GetString());
        Assert.Equal(
            "https://api.codes.cafe/api/notebooks/import/upload",
            structured
                .GetProperty("details")
                .GetProperty("recipe")
                .GetProperty("notebook")
                .GetProperty("url")
                .GetString()
        );
    }

    [Fact]
    public void TooLarge_MeasuresUtf8BytesRatherThanCharacters()
    {
        // A CJK character is three UTF-8 bytes: counting characters would let a payload through
        // at three times the limit the recipe advertises.
        var options = new McpOptions { MaxInlineContentBytes = 10 };
        var recipe = ImportGuidance.Recipe(Request(), options);

        Assert.NotNull(ImportGuidance.TooLarge(new string('中', 4), recipe));
        Assert.Null(ImportGuidance.TooLarge(new string('中', 3), recipe));
    }
}
