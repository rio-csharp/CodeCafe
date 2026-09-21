using CodeCafe.Application.Notebooks.GetNotebookSlugAvailability;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

public sealed class GetNotebookSlugAvailabilityQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReportsAvailable_WithNoSuggestions_WhenSlugIsFree()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetNotebookSlugAvailabilityQuery("free-slug"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("free-slug", result.Value!.Slug);
        Assert.True(result.Value.IsAvailable);
        Assert.Empty(result.Value.Suggestions);
    }

    [Fact]
    public async Task Handle_SuggestsFreeVariants_WhenSlugIsTaken()
    {
        var owner = SeedOwner();
        var handler = CreateHandler(SeedNotebook(owner, "taken-slug"));

        var result = await handler.Handle(new GetNotebookSlugAvailabilityQuery("taken-slug"), CancellationToken.None);

        Assert.False(result.Value!.IsAvailable);

        var suggestions = result.Value.Suggestions;
        Assert.Equal(3, suggestions.Count);
        Assert.Equal(suggestions.Count, suggestions.Distinct().Count());
        Assert.All(suggestions, suggestion => Assert.Matches(@"^taken-slug-\d{4}$", suggestion));
        // A suffixed slug still has to fit the column that stores it.
        Assert.All(suggestions, suggestion => Assert.True(suggestion.Length <= Notebook.MaxSlugLength));
    }

    [Fact]
    public async Task Handle_NormalizesTheRequestedSlug()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetNotebookSlugAvailabilityQuery("  My-Slug  "), CancellationToken.None);

        Assert.Equal("my-slug", result.Value!.Slug);
        Assert.True(result.Value.IsAvailable);
    }

    [Fact]
    public async Task Handle_IsCaseInsensitive()
    {
        var owner = SeedOwner();
        var handler = CreateHandler(SeedNotebook(owner, "my-slug"));

        var result = await handler.Handle(new GetNotebookSlugAvailabilityQuery("My-Slug"), CancellationToken.None);

        Assert.False(result.Value!.IsAvailable);
    }

    private static GetNotebookSlugAvailabilityQueryHandler CreateHandler(params Notebook[] notebooks)
    {
        var repository = new StubNotebookRepository();
        repository.AddRange(notebooks);
        return new GetNotebookSlugAvailabilityQueryHandler(repository);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, string slug)
        => Notebook.Create(owner.Id, "Title", null, slug, NotebookVisibility.Private);
}
