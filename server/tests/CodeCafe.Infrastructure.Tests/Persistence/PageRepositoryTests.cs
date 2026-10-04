using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Tests.Persistence;

[Collection(nameof(PostgresCollection))]
public sealed class PageRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Add_Save_And_Find_RoundTrips()
    {
        Page page;

        await using (var dbContext = await fixture.CreateCleanContextAsync())
        {
            var owner = SeedOwner(dbContext);
            var notebook = SeedNotebook(dbContext, owner);
            page = Page.Create(notebook.Id, null, "Getting Started", "getting-started", "a");
            page.Share(owner.Id, CollaboratorRole.Viewer);
            await new PageRepository(dbContext).AddAsync(page, TestContext.Current.CancellationToken);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = await fixture.CreateContextAsync();
        var repository = new PageRepository(verifyContext);

        var found = await repository.FindByIdAsync(page.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal("Getting Started", found.Title);
        Assert.Equal("getting-started", found.Slug);
        Assert.Equal("a", found.SortKey);
        Assert.Null(found.ParentId);
        Assert.Null(found.NextSiblingId);
        var share = Assert.Single(found.Shares);
        Assert.Equal(CollaboratorRole.Viewer, share.Role);
    }

    [Fact]
    public async Task Duplicate_Slug_InSameNotebook_ViolatesUniqueIndex()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var notebook = SeedNotebook(dbContext, SeedOwner(dbContext));
        var repository = new PageRepository(dbContext);
        await repository.AddAsync(Page.Create(notebook.Id, null, "One", "same-slug", "a"), TestContext.Current.CancellationToken);
        await repository.AddAsync(Page.Create(notebook.Id, null, "Two", "same-slug", "b"), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken)
        );

        Assert.Equal("IX_pages_NotebookId_Slug", exception.ConstraintName);
    }

    [Fact]
    public async Task Same_Slug_InDifferentNotebooks_IsFine()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var owner = SeedOwner(dbContext);
        var first = SeedNotebook(dbContext, owner, "first-notebook");
        var second = SeedNotebook(dbContext, owner, "second-notebook");
        var repository = new PageRepository(dbContext);
        await repository.AddAsync(Page.Create(first.Id, null, "Intro", "intro", "a"), TestContext.Current.CancellationToken);
        await repository.AddAsync(Page.Create(second.Id, null, "Intro", "intro", "a"), TestContext.Current.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(await repository.FindBySlugAsync(first.Id, "intro", TestContext.Current.CancellationToken));
        Assert.NotNull(await repository.FindBySlugAsync(second.Id, "intro", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SoftDeletedPages_AreHiddenByTheQueryFilter()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var notebook = SeedNotebook(dbContext, SeedOwner(dbContext));
        var repository = new PageRepository(dbContext);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        await repository.AddAsync(page, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        page.SoftDelete(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await repository.FindByIdAsync(page.Id, TestContext.Current.CancellationToken));
        Assert.Empty(await repository.ListByNotebookAsync(notebook.Id, TestContext.Current.CancellationToken));
        // The trashed page keeps its slug reserved until the trash slice settles restore semantics.
        Assert.Equal(1, await dbContext.Pages.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListSiblings_FiltersByParent_AndOrdersBySortKey()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var notebook = SeedNotebook(dbContext, SeedOwner(dbContext));
        var repository = new PageRepository(dbContext);
        var parent = Page.Create(notebook.Id, null, "Parent", "parent", "a");
        var second = Page.Create(notebook.Id, parent.Id, "Second", "second", "b");
        var first = Page.Create(notebook.Id, parent.Id, "First", "first", "a");
        await repository.AddAsync(parent, TestContext.Current.CancellationToken);
        await repository.AddAsync(second, TestContext.Current.CancellationToken);
        await repository.AddAsync(first, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var siblings = await repository.ListChildrenAsync(notebook.Id, parent.Id, TestContext.Current.CancellationToken);

        Assert.Equal([first.Id, second.Id], siblings.Select(sibling => sibling.Id));
    }

    [Fact]
    public async Task DeletingNotebook_CascadesPagesSharesAndFavorites()
    {
        Guid notebookId;
        await using (var dbContext = await fixture.CreateCleanContextAsync())
        {
            var owner = SeedOwner(dbContext);
            var notebook = SeedNotebook(dbContext, owner);
            notebookId = notebook.Id;
            var page = Page.Create(notebook.Id, null, "A", "a", "a");
            page.Share(owner.Id, CollaboratorRole.Viewer);
            var repository = new PageRepository(dbContext);
            await repository.AddAsync(page, TestContext.Current.CancellationToken);
            await repository.SetFavoriteAsync(page.Id, owner.Id, true, TestContext.Current.CancellationToken);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            dbContext.Notebooks.Remove(notebook);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = await fixture.CreateContextAsync();
        Assert.Equal(0, await verifyContext.Pages.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await verifyContext.Set<PageShare>().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await verifyContext.PageFavorites.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Favorites_RoundTrip_PerUser()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var owner = SeedOwner(dbContext);
        var notebook = SeedNotebook(dbContext, owner);
        var repository = new PageRepository(dbContext);
        var page = Page.Create(notebook.Id, null, "A", "a", "a");
        await repository.AddAsync(page, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.SetFavoriteAsync(page.Id, owner.Id, true, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var found = await repository.FindFavoriteIdsAsync(owner.Id, [page.Id], TestContext.Current.CancellationToken);
        Assert.Contains(page.Id, found);
        Assert.Empty(await repository.FindFavoriteIdsAsync(Guid.NewGuid(), [page.Id], TestContext.Current.CancellationToken));

        await repository.SetFavoriteAsync(page.Id, owner.Id, false, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Empty(await repository.FindFavoriteIdsAsync(owner.Id, [page.Id], TestContext.Current.CancellationToken));
    }

    private static User SeedOwner(AppDbContext dbContext)
    {
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        dbContext.Users.Add(owner);
        return owner;
    }

    private static Notebook SeedNotebook(AppDbContext dbContext, User owner, string slug = "my-notebook")
    {
        var notebook = Notebook.Create(owner.Id, "Notebook", null, slug, NotebookVisibility.Private);
        dbContext.Notebooks.Add(notebook);
        return notebook;
    }
}
