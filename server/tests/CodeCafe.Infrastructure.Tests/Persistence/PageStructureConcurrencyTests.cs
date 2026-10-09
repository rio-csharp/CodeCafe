using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Application.Pages.DeletePage;
using CodeCafe.Application.Pages.MovePage;
using CodeCafe.Application.Trash.RestorePageFromTrash;
using CodeCafe.Application.Trash.PurgeNotebook;
using CodeCafe.Application.Trash.PurgeTrashedNotebooks;
using CodeCafe.Application.Trash.RestoreNotebookFromTrash;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Tests.Persistence;

[Collection(nameof(PostgresCollection))]
public sealed class PageStructureConcurrencyTests(PostgresFixture fixture)
{
    [Fact]
    public async Task ConcurrentCreates_PreserveTheRootChain()
    {
        var (ownerId, notebookId, slug, _) = await SeedNotebookAsync();
        var gate = NewGate();

        var first = CreatePageAsync(ownerId, slug, "First", gate.Task);
        var second = CreatePageAsync(ownerId, slug, "Second", gate.Task);
        gate.SetResult();

        var results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        await AssertLiveRootChainAsync(notebookId, expectedCount: 2);
    }

    [Fact]
    public async Task ConcurrentMoveAndDelete_PreserveTheRemainingChain()
    {
        var (ownerId, notebookId, _, pages) = await SeedNotebookAsync("A", "B", "C");
        var a = pages[0];
        var b = pages[1];
        var c = pages[2];
        var gate = NewGate();

        var move = MovePageAsync(ownerId, c.Id, a.Id, gate.Task);
        var delete = DeletePageAsync(ownerId, b.Id, gate.Task);
        gate.SetResult();

        var outcomes = await Task.WhenAll(move, delete);

        Assert.All(outcomes, result => Assert.True(result));
        var live = await AssertLiveRootChainAsync(notebookId, expectedCount: 2);
        Assert.Equal([a.Id, c.Id], live.Select(page => page.Id));
    }

    [Fact]
    public async Task ConcurrentRestoreAndCreate_PreserveTheRootChainAndUniqueSlugs()
    {
        var (ownerId, notebookId, slug, pages) = await SeedNotebookAsync("Existing", "Restored");
        var trashed = pages[1];
        await using (var setup = await fixture.CreateContextAsync())
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var notebook = await setup.Notebooks.SingleAsync(candidate => candidate.Id == notebookId, cancellationToken);
            var all = await setup.Pages.Where(page => page.NotebookId == notebookId)
                .OrderBy(page => page.SortKey)
                .ToListAsync(cancellationToken);
            var trackedTrash = all.Single(page => page.Id == trashed.Id);
            PageChain.Unlink(trackedTrash, notebook, [], all);
            trackedTrash.SoftDelete(DateTimeOffset.UtcNow);
            await setup.SaveChangesAsync(cancellationToken);
        }

        var gate = NewGate();
        var restore = RestorePageAsync(ownerId, trashed.Id, gate.Task);
        var create = CreatePageAsync(ownerId, slug, "Restored", gate.Task);
        gate.SetResult();

        var restored = await restore;
        var created = await create;

        Assert.True(restored);
        Assert.True(created.IsSuccess);
        var live = await AssertLiveRootChainAsync(notebookId, expectedCount: 3);
        Assert.Equal(3, live.Select(page => page.Slug).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task CreateAfterAStaleTrackedTree_ReloadsStructureAfterTakingTheLock()
    {
        var (ownerId, notebookId, slug, pages) = await SeedNotebookAsync("Existing");
        await using var staleContext = await fixture.CreateContextAsync();
        var staleNotebooks = new NotebookRepository(staleContext);
        var stalePages = new PageRepository(staleContext);
        _ = await staleNotebooks.FindByIdAsync(notebookId, TestContext.Current.CancellationToken);
        var stalePage = Assert.Single(await stalePages.ListByNotebookAsync(
            notebookId,
            TestContext.Current.CancellationToken));

        var external = await CreatePageAsync(ownerId, slug, "External", Task.CompletedTask);
        Assert.True(external.IsSuccess);

        var handler = new CreatePageCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            staleNotebooks,
            stalePages,
            staleContext);
        var result = await handler.Handle(
            new CreatePageCommand(slug, "From stale scope", null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(EntityState.Detached, staleContext.Entry(stalePage).State);
        await AssertLiveRootChainAsync(notebookId, expectedCount: pages.Count + 2);
    }

    [Fact]
    public async Task PageStructureHandler_WaitsForAnExistingNotebookLock()
    {
        var (ownerId, _, slug, _) = await SeedNotebookAsync();
        await using var lockingContext = await fixture.CreateContextAsync();
        await using var transaction = await lockingContext.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await new NotebookRepository(lockingContext).LockPageStructureAsync(
            slug,
            TestContext.Current.CancellationToken);

        var blocked = CreatePageAsync(ownerId, slug, "Blocked", Task.CompletedTask);
        var timeout = Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

        Assert.Same(timeout, await Task.WhenAny(blocked, timeout));

        await transaction.CommitAsync(TestContext.Current.CancellationToken);
        Assert.True((await blocked).IsSuccess);
    }

    [Fact]
    public async Task UniqueViolationInsideTransaction_RollsBackToSavepointAndCanRetry()
    {
        var (_, _, slug, _) = await SeedNotebookAsync();
        await using var context = await fixture.CreateContextAsync();
        var owner = await context.Users.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        var duplicate = Notebook.Create(owner.Id, "Duplicate", null, slug, NotebookVisibility.Private);
        context.Notebooks.Add(duplicate);
        await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<CodeCafe.Application.Common.Exceptions.UniqueConstraintViolationException>(
            () => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        duplicate.ChangeSlug("retry-succeeds");
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await transaction.CommitAsync(TestContext.Current.CancellationToken);

        await using var verification = await fixture.CreateContextAsync();
        Assert.NotNull(await verification.Notebooks.SingleOrDefaultAsync(
            notebook => notebook.Slug == "retry-succeeds",
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConcurrentNotebookRestoreAndPurge_RecheckStateUnderTheLifecycleLock()
    {
        var (ownerId, notebookId) = await SeedTrashedNotebookAsync();
        var gate = NewGate();
        var restore = RestoreNotebookAsync(ownerId, notebookId, gate.Task);
        var purge = PurgeNotebookAsync(ownerId, notebookId, gate.Task);
        gate.SetResult();

        var restored = await restore;
        var purged = await purge;

        Assert.NotEqual(restored, purged);
        await using var verification = await fixture.CreateContextAsync();
        var notebook = await verification.Notebooks.IgnoreQueryFilters().SingleOrDefaultAsync(
            candidate => candidate.Id == notebookId,
            TestContext.Current.CancellationToken);
        Assert.Equal(restored, notebook is { DeletedAtUtc: null });
    }

    [Fact]
    public async Task ConcurrentNotebookRestoreAndEmptyTrash_DoesNotDeleteARestoredNotebook()
    {
        var (ownerId, notebookId) = await SeedTrashedNotebookAsync();
        var gate = NewGate();
        var restore = RestoreNotebookAsync(ownerId, notebookId, gate.Task);
        var purgeAll = PurgeAllNotebooksAsync(ownerId, gate.Task);
        gate.SetResult();

        var restored = await restore;
        Assert.True(await purgeAll);

        await using var verification = await fixture.CreateContextAsync();
        var notebook = await verification.Notebooks.IgnoreQueryFilters().SingleOrDefaultAsync(
            candidate => candidate.Id == notebookId,
            TestContext.Current.CancellationToken);
        if (restored)
        {
            Assert.NotNull(notebook);
            Assert.Null(notebook.DeletedAtUtc);
        }
        else
        {
            Assert.Null(notebook);
        }
    }

    [Fact]
    public async Task NotebookRestore_WaitsForLifecycleLock()
    {
        var (ownerId, notebookId) = await SeedTrashedNotebookAsync();
        await using var lockingContext = await fixture.CreateContextAsync();
        await using var transaction = await lockingContext.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await new NotebookRepository(lockingContext).LockLifecycleAsync(
            notebookId,
            TestContext.Current.CancellationToken);

        var restore = RestoreNotebookAsync(ownerId, notebookId, Task.CompletedTask);
        await AssertBlockedAsync(restore);

        await transaction.CommitAsync(TestContext.Current.CancellationToken);
        Assert.True(await restore);
    }

    [Fact]
    public async Task LateNotebookPurge_RechecksAfterLockAndDoesNotDeleteRestoredNotebook()
    {
        var (ownerId, notebookId) = await SeedTrashedNotebookAsync();
        await using var lockingContext = await fixture.CreateContextAsync();
        await using var transaction = await lockingContext.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        var repository = new NotebookRepository(lockingContext);
        await repository.LockLifecycleAsync(notebookId, TestContext.Current.CancellationToken);
        var notebook = await repository.FindTrashedByIdAsync(notebookId, TestContext.Current.CancellationToken);
        Assert.NotNull(notebook);
        notebook.Restore();
        await lockingContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var purge = PurgeNotebookAsync(ownerId, notebookId, Task.CompletedTask);
        await AssertBlockedAsync(purge);

        await transaction.CommitAsync(TestContext.Current.CancellationToken);
        Assert.False(await purge);
        await AssertNotebookIsLiveAsync(notebookId);
    }

    [Fact]
    public async Task LateEmptyTrash_RechecksAfterLockAndSkipsRestoredNotebook()
    {
        var (ownerId, notebookId) = await SeedTrashedNotebookAsync();
        await using var lockingContext = await fixture.CreateContextAsync();
        await using var transaction = await lockingContext.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        var repository = new NotebookRepository(lockingContext);
        await repository.LockLifecycleAsync(notebookId, TestContext.Current.CancellationToken);
        var notebook = await repository.FindTrashedByIdAsync(notebookId, TestContext.Current.CancellationToken);
        Assert.NotNull(notebook);

        var purgeAll = PurgeAllNotebooksAsync(ownerId, Task.CompletedTask);
        await AssertBlockedAsync(purgeAll);

        notebook.Restore();
        await lockingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await transaction.CommitAsync(TestContext.Current.CancellationToken);

        Assert.True(await purgeAll);
        await AssertNotebookIsLiveAsync(notebookId);
    }

    private async Task<(Guid OwnerId, Guid NotebookId, string Slug, IReadOnlyList<Page> Pages)> SeedNotebookAsync(
        params string[] pageTitles)
    {
        await using var context = await fixture.CreateCleanContextAsync();
        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        var notebook = Notebook.Create(owner.Id, "Notebook", null, "notebook", NotebookVisibility.Private);
        context.Users.Add(owner);
        context.Notebooks.Add(notebook);

        var pages = new List<Page>();
        foreach (var title in pageTitles)
        {
            var page = Page.Create(
                notebook.Id,
                null,
                title,
                title.ToLowerInvariant(),
                SiblingSortKeys.KeyForInsert(pages, pages.Count));
            PageChain.Link(page, notebook, null, pages.Count == 0 ? null : pages[^1]);
            pages.Add(page);
            context.Pages.Add(page);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (owner.Id, notebook.Id, notebook.Slug, pages);
    }

    private async Task<(Guid OwnerId, Guid NotebookId)> SeedTrashedNotebookAsync()
    {
        var (ownerId, notebookId, _, _) = await SeedNotebookAsync();
        await using var context = await fixture.CreateContextAsync();
        var notebook = await context.Notebooks.SingleAsync(
            candidate => candidate.Id == notebookId,
            TestContext.Current.CancellationToken);
        notebook.SoftDelete(DateTimeOffset.UtcNow);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (ownerId, notebookId);
    }

    private async Task<CodeCafe.Application.Common.Result<CodeCafe.Application.Pages.Shared.PageDetailsDto>> CreatePageAsync(
        Guid ownerId,
        string notebookSlug,
        string title,
        Task gate)
    {
        await using var context = await fixture.CreateContextAsync();
        var handler = new CreatePageCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            new PageRepository(context),
            context);
        await gate;
        return await handler.Handle(new CreatePageCommand(notebookSlug, title, null), TestContext.Current.CancellationToken);
    }

    private async Task<bool> MovePageAsync(Guid ownerId, Guid pageId, Guid afterPageId, Task gate)
    {
        await using var context = await fixture.CreateContextAsync();
        var handler = new MovePageCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            new PageRepository(context),
            new UserRepository(context),
            context);
        await gate;
        return (await handler.Handle(
            new MovePageCommand(pageId, null, afterPageId),
            TestContext.Current.CancellationToken)).IsSuccess;
    }

    private async Task<bool> DeletePageAsync(Guid ownerId, Guid pageId, Task gate)
    {
        await using var context = await fixture.CreateContextAsync();
        var handler = new DeletePageCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            new PageRepository(context),
            context);
        await gate;
        return (await handler.Handle(new DeletePageCommand(pageId), TestContext.Current.CancellationToken)).IsSuccess;
    }

    private async Task<bool> RestorePageAsync(Guid ownerId, Guid pageId, Task gate)
    {
        await using var context = await fixture.CreateContextAsync();
        var handler = new RestorePageFromTrashCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            new PageRepository(context),
            context);
        await gate;
        return (await handler.Handle(new RestorePageFromTrashCommand(pageId), TestContext.Current.CancellationToken)).IsSuccess;
    }

    private async Task<bool> RestoreNotebookAsync(Guid ownerId, Guid notebookId, Task gate)
    {
        await using var context = await fixture.CreateContextAsync();
        var handler = new RestoreNotebookFromTrashCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            context);
        await gate;
        return (await handler.Handle(
            new RestoreNotebookFromTrashCommand(notebookId),
            TestContext.Current.CancellationToken)).IsSuccess;
    }

    private async Task<bool> PurgeNotebookAsync(Guid ownerId, Guid notebookId, Task gate)
    {
        await using var context = await fixture.CreateContextAsync();
        var handler = new PurgeNotebookCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            context);
        await gate;
        return (await handler.Handle(
            new PurgeNotebookCommand(notebookId),
            TestContext.Current.CancellationToken)).IsSuccess;
    }

    private async Task<bool> PurgeAllNotebooksAsync(Guid ownerId, Task gate)
    {
        await using var context = await fixture.CreateContextAsync();
        var handler = new PurgeTrashedNotebooksCommandHandler(
            new TestCurrentUserAccessor(ownerId),
            new NotebookRepository(context),
            context);
        await gate;
        return (await handler.Handle(
            new PurgeTrashedNotebooksCommand(),
            TestContext.Current.CancellationToken)).IsSuccess;
    }

    private async Task<IReadOnlyList<Page>> AssertLiveRootChainAsync(Guid notebookId, int expectedCount)
    {
        await using var context = await fixture.CreateContextAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var notebook = await context.Notebooks.SingleAsync(candidate => candidate.Id == notebookId, cancellationToken);
        var live = await context.Pages
            .Where(page => page.NotebookId == notebookId && page.ParentId == null)
            .OrderBy(page => page.SortKey)
            .ToListAsync(cancellationToken);
        Assert.Equal(expectedCount, live.Count);
        Assert.Equal(expectedCount, live.Select(page => page.SortKey).Distinct(StringComparer.Ordinal).Count());

        var byId = live.ToDictionary(page => page.Id);
        var chain = new List<Page>();
        var visited = new HashSet<Guid>();
        var cursor = notebook.FirstPageId;
        while (cursor is { } id && visited.Add(id) && byId.TryGetValue(id, out var page))
        {
            chain.Add(page);
            cursor = page.NextSiblingId;
        }

        Assert.Null(cursor);
        Assert.Equal(live.Select(page => page.Id), chain.Select(page => page.Id));
        return live;
    }

    private async Task AssertNotebookIsLiveAsync(Guid notebookId)
    {
        await using var context = await fixture.CreateContextAsync();
        var notebook = await context.Notebooks.SingleOrDefaultAsync(
            candidate => candidate.Id == notebookId,
            TestContext.Current.CancellationToken);
        Assert.NotNull(notebook);
        Assert.Null(notebook.DeletedAtUtc);
    }

    private static async Task AssertBlockedAsync(Task operation)
    {
        var timeout = Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
        Assert.Same(timeout, await Task.WhenAny(operation, timeout));
    }

    private static TaskCompletionSource NewGate()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TestCurrentUserAccessor(Guid userId) : ICurrentUserAccessor
    {
        public CurrentUser? User { get; } = new(userId);
    }
}
