using CodeCafe.Application.Auth;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages.ListFavoritePages;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests;

public sealed class ListFavoritePagesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTheCallersFavorites_WithNotebookTitles()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "Recipes", "recipes");
        var starred = Page.Create(notebook.Id, null, "Starred", "starred", "a");
        var plain = Page.Create(notebook.Id, null, "Plain", "plain", "b");
        var (handler, pages) = CreateHandler(owner.Id, [notebook], [starred, plain]);
        await pages.SetFavoriteAsync(starred.Id, owner.Id, true, CancellationToken.None);

        var result = await handler.Handle(new ListFavoritePagesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value!);
        Assert.Equal(starred.Id, dto.PageId);
        Assert.Equal("Starred", dto.Title);
        Assert.Equal(notebook.Id, dto.NotebookId);
        Assert.Equal("Recipes", dto.NotebookTitle);
    }

    [Fact]
    public async Task Handle_FiltersByNotebookId()
    {
        var owner = SeedOwner();
        var recipes = SeedNotebook(owner, "Recipes", "recipes");
        var work = SeedNotebook(owner, "Work", "work");
        var inRecipes = Page.Create(recipes.Id, null, "InRecipes", "in-recipes", "a");
        var inWork = Page.Create(work.Id, null, "InWork", "in-work", "a");
        var (handler, pages) = CreateHandler(owner.Id, [recipes, work], [inRecipes, inWork]);
        await pages.SetFavoriteAsync(inRecipes.Id, owner.Id, true, CancellationToken.None);
        await pages.SetFavoriteAsync(inWork.Id, owner.Id, true, CancellationToken.None);

        var result = await handler.Handle(new ListFavoritePagesQuery(recipes.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value!);
        Assert.Equal(inRecipes.Id, dto.PageId);
    }

    [Fact]
    public async Task Handle_ExcludesOtherUsersFavorites()
    {
        var owner = SeedOwner();
        var other = User.Create("other@example.com", "other@example.com", "Other", "hash");
        var notebook = SeedNotebook(owner, "Recipes", "recipes");
        var page = Page.Create(notebook.Id, null, "P", "p", "a");
        var (handler, pages) = CreateHandler(owner.Id, [notebook], [page]);
        await pages.SetFavoriteAsync(page.Id, other.Id, true, CancellationToken.None);

        var result = await handler.Handle(new ListFavoritePagesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_SkipsTrashedPages()
    {
        var owner = SeedOwner();
        var notebook = SeedNotebook(owner, "Recipes", "recipes");
        var page = Page.Create(notebook.Id, null, "P", "p", "a");
        var (handler, pages) = CreateHandler(owner.Id, [notebook], [page]);
        await pages.SetFavoriteAsync(page.Id, owner.Id, true, CancellationToken.None);
        page.SoftDelete(DateTimeOffset.UtcNow);

        var result = await handler.Handle(new ListFavoritePagesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_AnonymousGetsUserNotFound()
    {
        var handler = new ListFavoritePagesQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubNotebookRepository(),
            new StubPageRepository()
        );

        var result = await handler.Handle(new ListFavoritePagesQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    private static User SeedOwner() => User.Create("owner@example.com", "owner@example.com", "Owner", "hash");

    private static Notebook SeedNotebook(User owner, string title, string slug)
        => Notebook.Create(owner.Id, title, null, slug, NotebookVisibility.Private);

    private static (ListFavoritePagesQueryHandler Handler, StubPageRepository Pages) CreateHandler(
        Guid currentUserId,
        Notebook[] notebooks,
        Page[] seededPages
    )
    {
        var pages = new StubPageRepository();
        pages.AddRange(seededPages);
        var notebookRepository = new StubNotebookRepository();
        notebookRepository.AddRange(notebooks);
        var handler = new ListFavoritePagesQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(currentUserId)),
            notebookRepository,
            pages
        );
        return (handler, pages);
    }
}
