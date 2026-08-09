using CodeCafe.Application.Identity;
using CodeCafe.Host.Rest.Auth;
using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Host.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CodeCafe.Api.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        // A singleton that captures a scoped service silently reads stale state instead of failing,
        // which is how the read-service double ended up unable to see writes from the current request.
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            // Registration defaults to disabled; endpoint tests exercise the
            // register flow, so enable it explicitly for the test host.
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:RegistrationEnabled"] = "true"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });

            // Scoped, not singleton: this double holds mutable seed data, and the delete tests really
            // remove the seeded notebook. As a singleton it leaked that mutation into every later test
            // in the class fixture, so the suite only passed because the destructive tests happened to
            // be declared last. Per-request scope restores the seed for each test.
            services.AddScoped<TestNotebookMutationStore>();
            services.AddScoped<INotebookMutationStore>(serviceProvider => serviceProvider.GetRequiredService<TestNotebookMutationStore>());
            // Scoped as well: this double reads through TestNotebookMutationStore, so as a singleton it
            // captured one scope's store and could not see writes made in the current request's scope.
            services.AddScoped<INotebookReadService, TestNotebookQueryService>();
            // Stateless and store-independent, so a singleton is fine here.
            services.AddSingleton<INotebookItemMutationService, TestNotebookCommandService>();
            services.AddSingleton<IAuthUserGateway, TestAuthUserGateway>();
            services.AddSingleton<IAuthSessionService, TestAuthSessionService>();
        });
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues)
            || !Guid.TryParse(userIdValues.SingleOrDefault(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class TestNotebookQueryService(TestNotebookMutationStore notebookMutationStore) : INotebookReadService
{
    private static readonly Guid NotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ItemId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly NotebookItemModel[] Items =
    [
        new(
            ItemId,
            NotebookId,
            null,
            "page",
            "Overview",
            "overview",
            "overview",
            1,
            "tiptap_json",
            null,
            "Overview content",
            false,
            null,
            null,
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
    ];

    private static readonly NotebookDetailModel PublicNotebook = new(
        NotebookId,
        OwnerId,
        "Architecture Notes",
        "architecture-notes",
        "Refactor plan",
        "public",
        true,
        "Yao",
        false,
        2,
        1,
        1,
        0,
        false,
        DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
        DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
        DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
        DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
        Items);

    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(string? search, Guid currentUserId, CancellationToken cancellationToken, int? limit = null, int? offset = null)
    {
        IReadOnlyList<NotebookSummaryModel> result =
        [
            new(
                NotebookId,
                OwnerId,
                "Architecture Notes",
                "architecture-notes",
                "Refactor plan",
                "public",
                true,
                "Yao",
                false,
                2,
                1,
                1,
                0,
                false,
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
        ];

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(Guid currentUserId, string? search, CancellationToken cancellationToken, int? limit = null, int? offset = null)
        => Task.FromResult<IReadOnlyList<NotebookSummaryModel>>(
        [
            new(
                NotebookId,
                currentUserId,
                "My Notes",
                "my-notes",
                "Owned notebook",
                "private",
                false,
                "Yao",
                true,
                1,
                0,
                1,
                0,
                false,
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                null)
        ]);

    public Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(Guid currentUserId, string search, CancellationToken cancellationToken, int? limit = null)
        => Task.FromResult<IReadOnlyList<NotebookItemSearchModel>>([]);

    public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookDetailModel>.Success(includeItems ? PublicNotebook : PublicNotebook with { Items = [] })
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(Items)
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
            && string.Equals(path, "overview", StringComparison.Ordinal)
                ? NotesResult<NotebookItemModel>.Success(Items[0])
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
        => Task.FromResult(
            notebookMutationStore.TryGetNotebookDetail(notebookId, currentUserId, out var notebookDetail)
                ? NotesResult<NotebookDetailModel>.Success(notebookDetail)
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
        => Task.FromResult(
            notebookMutationStore.TryGetNotebookDetailBySlug(slug, currentUserId, out var notebookDetail)
                ? NotesResult<NotebookDetailModel>.Success(notebookDetail)
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(Guid notebookId, Guid currentUserId, string? search, CancellationToken cancellationToken, bool includeArchived = false, bool includeContent = true, int? limit = null)
        => Task.FromResult(
            notebookId == NotebookId
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(Items)
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookItemModel>> GetNotebookItemByIdAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            notebookId == NotebookId
            && Items.SingleOrDefault(item => item.Id == itemId) is { } item
                ? NotesResult<NotebookItemModel>.Success(item)
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found."));
}

internal sealed class TestNotebookCommandService : INotebookItemMutationService
{
    private static readonly Guid DefaultItemId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(Guid notebookId, Guid currentUserId, Guid? parentId, string type, string title, int sortOrder, System.Text.Json.JsonElement? contentJson, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(
            new NotebookItemModel(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                notebookId,
                parentId,
                type,
                title,
                "new-item",
                "new-item",
                sortOrder,
                type == "page" ? "tiptap_json" : null,
                contentJson,
                type == "page" ? "Created content" : null,
                false,
                null,
                null,
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))));

    public Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, string title, System.Text.Json.JsonElement parentId, int? sortOrder, System.Text.Json.JsonElement contentJson, CancellationToken cancellationToken, DateTimeOffset? expectedUpdatedAtUtc = null)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(
            new NotebookItemModel(
                itemId,
                notebookId,
                null,
                "page",
                title,
                "updated-item",
                "updated-item",
                sortOrder ?? 1,
                "tiptap_json",
                contentJson,
                "Updated content",
                false,
                null,
                null,
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(Guid notebookId, Guid currentUserId, IReadOnlyList<ReorderNotebookItemModel> items, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Success(
        [
            new NotebookItemModel(
                DefaultItemId,
                notebookId,
                items[0].ParentId,
                "page",
                "Overview",
                "overview",
                "overview",
                items[0].SortOrder,
                "tiptap_json",
                null,
                "Overview content",
                false,
                null,
                null,
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
        ]));

    public Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult.Success());

    public Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(
            new NotebookItemModel(
                itemId,
                notebookId,
                null,
                "page",
                "Overview",
                "overview",
                "overview",
                1,
                "tiptap_json",
                null,
                "Overview content",
                true,
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                currentUserId,
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))));

    public Task<NotesResult<NotebookItemModel>> RestoreNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(
            new NotebookItemModel(
                itemId,
                notebookId,
                null,
                "page",
                "Overview",
                "overview",
                "overview",
                1,
                "tiptap_json",
                null,
                "Overview content",
                false,
                null,
                null,
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))));
}

internal sealed class TestNotebookMutationStore : INotebookMutationStore
{
    private static readonly Guid DefaultNotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DefaultOwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Dictionary<Guid, Notebook> _notebooks;
    private readonly List<NotebookFavorite> _favorites;

    public TestNotebookMutationStore()
    {
        _notebooks = new Dictionary<Guid, Notebook>
        {
            [DefaultNotebookId] = new Notebook
            {
                Id = DefaultNotebookId,
                OwnerId = DefaultOwnerId,
                Title = "Architecture Notes",
                Slug = "architecture-notes",
                Description = "Refactor plan",
                Visibility = NotebookVisibility.Public,
                IsPublished = true,
                CreatedAtUtc = DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                UpdatedAtUtc = DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                PublishedAtUtc = DateTimeOffset.Parse("2026-06-01T00:00:00+00:00")
            }
        };
        _favorites =
        [
            new NotebookFavorite
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                NotebookId = DefaultNotebookId,
                UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            }
        ];
    }

    public void AddNotebook(Notebook notebook) => _notebooks[notebook.Id] = notebook;

    public Task<NotebookFavorite?> GetFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(_favorites.SingleOrDefault(favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId));

    public Task<Notebook?> GetNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(_notebooks.GetValueOrDefault(notebookId));

    public Task<Notebook?> GetOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(_notebooks.TryGetValue(notebookId, out var notebook) && notebook.OwnerId == currentUserId ? notebook : null);

    public Task<bool> NotebookExistsAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(_notebooks.ContainsKey(notebookId));

    public Task<string> GenerateUniqueNotebookSlugAsync(string title, Guid? currentNotebookId, CancellationToken cancellationToken)
    {
        var baseSlug = string.Join('-', title.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (_notebooks.Values.All(notebook => notebook.Id == currentNotebookId || notebook.Slug != baseSlug))
        {
            return Task.FromResult(baseSlug);
        }

        return Task.FromResult($"{baseSlug}-{_notebooks.Count}");
    }

    public void RemoveNotebook(Notebook notebook) => _notebooks.Remove(notebook.Id);

    public Task SaveNotebookAsync(Notebook notebook, string title, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<int> CountFavoritesAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(_favorites.Count(favorite => favorite.NotebookId == notebookId));

    public Task<bool> IsFavoritedAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(_favorites.Any(favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId));

    public Task AddFavoriteAsync(NotebookFavorite favorite, CancellationToken cancellationToken)
    {
        _favorites.Add(favorite);
        return Task.CompletedTask;
    }

    public void RemoveFavorite(NotebookFavorite favorite) => _favorites.Remove(favorite);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public bool TryGetNotebookDetail(Guid notebookId, Guid currentUserId, out NotebookDetailModel notebookDetail)
    {
        if (_notebooks.TryGetValue(notebookId, out var notebook))
        {
            notebookDetail = ToDetail(notebook, currentUserId);
            return true;
        }

        notebookDetail = null!;
        return false;
    }

    public bool TryGetNotebookDetailBySlug(string slug, Guid currentUserId, out NotebookDetailModel notebookDetail)
    {
        var notebook = _notebooks.Values.SingleOrDefault(existingNotebook => string.Equals(existingNotebook.Slug, slug, StringComparison.Ordinal));
        if (notebook is not null)
        {
            notebookDetail = ToDetail(notebook, currentUserId);
            return true;
        }

        notebookDetail = null!;
        return false;
    }

    private NotebookDetailModel ToDetail(Notebook notebook, Guid currentUserId)
    {
        return new NotebookDetailModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.Visibility.ToString().ToLowerInvariant(),
            notebook.IsPublished,
            "Yao",
            notebook.OwnerId == currentUserId,
            0,
            0,
            0,
            _favorites.Count(favorite => favorite.NotebookId == notebook.Id),
            _favorites.Any(favorite => favorite.NotebookId == notebook.Id && favorite.UserId == currentUserId),
            notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc,
            notebook.PublishedAtUtc,
            []);
    }
}

internal sealed class TestAuthUserGateway : IAuthUserGateway
{
    private static readonly Guid DefaultUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public Task<AuthUserModel?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        AuthUserModel? user = normalizedEmail switch
        {
            "yao@example.com" => new AuthUserModel(DefaultUserId, normalizedEmail, "Yao"),
            "existing.user@example.com" => new AuthUserModel(DefaultUserId, normalizedEmail, "Existing User"),
            _ => null
        };

        return Task.FromResult(user);
    }

    public Task<AuthUserModel?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<AuthUserModel?>(
            userId == Guid.Empty
                ? null
                : new AuthUserModel(userId, "yao@example.com", "Yao"));
    }

    public Task<AuthCreateUserResult> CreateUserAsync(
        string normalizedEmail,
        string displayName,
        string password,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            string.Equals(normalizedEmail, "existing.user@example.com", StringComparison.Ordinal)
                ? AuthCreateUserResult.Failure(["DuplicateEmail"])
                : AuthCreateUserResult.Success(new AuthUserModel(DefaultUserId, normalizedEmail, displayName)));
    }

    public async Task<AuthPasswordVerificationResult> VerifyPasswordAsync(
        string normalizedEmail,
        string password,
        bool lockoutOnFailure,
        CancellationToken cancellationToken)
    {
        var user = await FindByEmailAsync(normalizedEmail, cancellationToken);
        return user is not null && password == "Password123!"
            ? AuthPasswordVerificationResult.Success(user)
            : AuthPasswordVerificationResult.Failure(isLockedOut: false);
    }
}

internal sealed class TestAuthSessionService : IAuthSessionService
{
    public Task SignInAsync(Guid userId, bool isPersistent) => Task.CompletedTask;

    public Task SignOutAsync() => Task.CompletedTask;
}
