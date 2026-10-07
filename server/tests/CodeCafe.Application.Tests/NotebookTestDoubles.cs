using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests;

// Shared doubles for the notebook handler tests; auth handler tests keep their own local stubs.
internal sealed class StubCurrentUserAccessor(CurrentUser? user) : ICurrentUserAccessor
{
    public CurrentUser? User => user;
}

internal sealed class StubUserRepository : List<User>, IUserRepository
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => Task.FromResult(this.FirstOrDefault(user => user.NormalizedEmail == normalizedEmail));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(this.FirstOrDefault(user => user.Id == id));

    public Task<IReadOnlyList<User>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<User>>(this.Where(user => ids.Contains(user.Id)).ToList());

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class StubNotebookRepository : List<Notebook>, INotebookRepository
{
    public HashSet<(Guid NotebookId, Guid UserId)> Favorites { get; } = [];

    // Page shares that surface the notebook in the list, mirroring the EF query.
    public List<(Guid NotebookId, Guid UserId)> PageShareGrants { get; } = [];

    // Mirrors the EF global query filter: trashed notebooks are invisible outside trash queries.
    private IEnumerable<Notebook> Live => this.Where(notebook => notebook.DeletedAtUtc == null);

    public Task<Notebook?> FindBySlugAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(Live.FirstOrDefault(notebook => notebook.Slug == slug));

    public Task<Notebook?> FindByIdAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(Live.FirstOrDefault(notebook => notebook.Id == notebookId));

    public Task<IReadOnlyList<Notebook>> FindByIdsAsync(IReadOnlyCollection<Guid> notebookIds, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Notebook>>(Live.Where(notebook => notebookIds.Contains(notebook.Id)).ToList());

    public Task<Notebook?> FindByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken)
        => Guid.TryParse(idOrSlug, out var id)
            ? FindByIdAsync(id, cancellationToken)
            : Task.FromResult(Live.FirstOrDefault(notebook => notebook.Slug == idOrSlug.Trim().ToLowerInvariant()));

    public Task<int> CountVisibleAsync(Guid? userId, NotebookFilter filter, CancellationToken cancellationToken)
        => Task.FromResult(VisibleTo(userId, filter).Count());

    public Task<IReadOnlyList<Notebook>> ListVisibleAsync(
        Guid? userId,
        NotebookFilter filter,
        NotebookSort sort,
        int skip,
        int take,
        CancellationToken cancellationToken
    )
    {
        var query = VisibleTo(userId, filter);

        var ordered = sort switch
        {
            NotebookSort.TitleAsc => query.OrderBy(notebook => notebook.Title, StringComparer.Ordinal).ThenBy(notebook => notebook.Id),
            NotebookSort.CreatedDesc => query
                .OrderByDescending(notebook => notebook.CreatedAtUtc)
                .ThenByDescending(notebook => notebook.Id),
            _ => query.OrderByDescending(notebook => notebook.UpdatedAtUtc).ThenByDescending(notebook => notebook.Id),
        };

        return Task.FromResult<IReadOnlyList<Notebook>>(ordered.Skip(skip).Take(take).ToList());
    }

    private IEnumerable<Notebook> VisibleTo(Guid? userId, NotebookFilter filter)
    {
        IEnumerable<Notebook> query = userId is null
            ? Live.Where(notebook => notebook.Visibility == NotebookVisibility.Public)
            : Live.Where(notebook =>
                (filter.IsFavorite == true && notebook.Visibility == NotebookVisibility.Public)
                || notebook.OwnerId == userId
                || notebook.IsSharedWith(userId.Value)
                || PageShareGrants.Any(grant => grant.NotebookId == notebook.Id && grant.UserId == userId)
            );
        if (filter.Tag is not null)
        {
            query = query.Where(notebook => notebook.Tags.Contains(filter.Tag));
        }
        if (filter.IsFavorite is not null && userId is not null)
        {
            query = query.Where(notebook => Favorites.Contains((notebook.Id, userId.Value)) == filter.IsFavorite.Value);
        }
        if (filter.Visibility is not null)
        {
            query = query.Where(notebook => notebook.Visibility == filter.Visibility.Value);
        }
        if (filter.Search is not null)
        {
            // Mirrors the ILIKE the real repository issues: case-insensitive substring match.
            query = query.Where(notebook =>
                notebook.Title.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || (
                    notebook.Description is not null
                    && notebook.Description.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                )
            );
        }

        return query;
    }

    // The owned + notebook-share visibility arms without filters: what page search scopes to.
    public IEnumerable<Notebook> NotebooksVisibleTo(Guid userId)
        => Live.Where(notebook => notebook.OwnerId == userId || notebook.IsSharedWith(userId));

    public Task SetFavoriteAsync(Guid notebookId, Guid userId, bool isFavorite, CancellationToken cancellationToken)
    {
        if (isFavorite)
        {
            Favorites.Add((notebookId, userId));
        }
        else
        {
            Favorites.Remove((notebookId, userId));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlySet<Guid>> FindFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> notebookIds,
        CancellationToken cancellationToken
    ) => Task.FromResult<IReadOnlySet<Guid>>(
        Favorites.Where(favorite => favorite.UserId == userId && notebookIds.Contains(favorite.NotebookId))
            .Select(favorite => favorite.NotebookId)
            .ToHashSet()
    );

    public Task AddAsync(Notebook notebook, CancellationToken cancellationToken)
    {
        Add(notebook);
        return Task.CompletedTask;
    }

    public Task<Notebook?> FindTrashedByIdAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(this.FirstOrDefault(notebook => notebook.Id == notebookId && notebook.DeletedAtUtc != null));

    public Task<IReadOnlyList<Notebook>> ListTrashedAsync(Guid ownerId, int skip, int take, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Notebook>>(
            this.Where(notebook => notebook.OwnerId == ownerId && notebook.DeletedAtUtc != null)
                .OrderByDescending(notebook => notebook.DeletedAtUtc)
                .Skip(skip)
                .Take(take)
                .ToList()
        );

    public Task<int> CountTrashedAsync(Guid ownerId, CancellationToken cancellationToken)
        => Task.FromResult(this.Count(notebook => notebook.OwnerId == ownerId && notebook.DeletedAtUtc != null));

    // Hides List<Notebook>.Remove to satisfy the repository interface; the cast calls the base.
    public new void Remove(Notebook notebook) => ((List<Notebook>)this).Remove(notebook);
}

internal sealed class StubUnitOfWork(Exception? saveFailure = null) : IUnitOfWork
{
    private int _failuresRemaining = saveFailure is null ? 0 : int.MaxValue;

    public int SaveChangesCallCount { get; private set; }

    public int FailuresRemaining
    {
        get => _failuresRemaining;
        set => _failuresRemaining = value;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        if (_failuresRemaining > 0)
        {
            _failuresRemaining--;
            return Task.FromException(saveFailure!);
        }

        return Task.CompletedTask;
    }

    public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => Task.FromResult<ITransaction>(StubTransaction.Instance);
}

// No database, no transaction: commit and dispose are no-ops.
internal sealed class StubTransaction : ITransaction
{
    public static readonly StubTransaction Instance = new();

    private StubTransaction() { }

    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class StubPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
}
