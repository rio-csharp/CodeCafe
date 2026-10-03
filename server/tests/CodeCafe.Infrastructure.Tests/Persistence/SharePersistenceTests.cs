using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Tests.Persistence;

// Sharing mutates an already-tracked aggregate: the new share entity is discovered through the
// collection navigation, where EF classifies client-keyed newcomers as existing rows.
[Collection(nameof(PostgresCollection))]
public sealed class SharePersistenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Share_OnLoadedNotebook_InsertsTheShareRow()
    {
        Guid notebookId;
        Guid targetId;

        await using (var setup = await fixture.CreateCleanContextAsync())
        {
            var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
            var target = User.Create("target@example.com", "target@example.com", "Target", "hash");
            var notebook = Notebook.Create(owner.Id, "Shared Book", null, "shared-book", NotebookVisibility.Private);
            setup.Set<User>().AddRange(owner, target);
            setup.Set<Notebook>().Add(notebook);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);
            notebookId = notebook.Id;
            targetId = target.Id;
        }

        await using (var acting = await fixture.CreateContextAsync())
        {
            var notebook = await acting.Set<Notebook>().SingleAsync(
                n => n.Id == notebookId,
                TestContext.Current.CancellationToken
            );
            notebook.Share(targetId, CollaboratorRole.Editor);
            await acting.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verify = await fixture.CreateContextAsync();
        var persisted = await verify
            .Set<NotebookShare>()
            .Where(share => share.NotebookId == notebookId)
            .ToListAsync(TestContext.Current.CancellationToken);
        var share = Assert.Single(persisted);
        Assert.Equal(targetId, share.UserId);
        Assert.Equal(CollaboratorRole.Editor, share.Role);
    }

    [Fact]
    public async Task Share_OnLoadedPage_InsertsTheShareRow()
    {
        Guid pageId;
        Guid targetId;

        await using (var setup = await fixture.CreateCleanContextAsync())
        {
            var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
            var target = User.Create("target@example.com", "target@example.com", "Target", "hash");
            var notebook = Notebook.Create(owner.Id, "Book", null, "book", NotebookVisibility.Private);
            var page = Page.Create(notebook.Id, null, "Page", "page", "a");
            setup.Set<User>().AddRange(owner, target);
            setup.Set<Notebook>().Add(notebook);
            setup.Set<Page>().Add(page);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);
            pageId = page.Id;
            targetId = target.Id;
        }

        await using (var acting = await fixture.CreateContextAsync())
        {
            var page = await acting.Set<Page>().SingleAsync(
                p => p.Id == pageId,
                TestContext.Current.CancellationToken
            );
            page.Share(targetId, CollaboratorRole.Viewer);
            await acting.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verify = await fixture.CreateContextAsync();
        var persisted = await verify
            .Set<PageShare>()
            .Where(share => share.PageId == pageId)
            .ToListAsync(TestContext.Current.CancellationToken);
        var share = Assert.Single(persisted);
        Assert.Equal(targetId, share.UserId);
    }
}
