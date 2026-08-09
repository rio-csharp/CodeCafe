using CodeCafe.Infrastructure.Ai;
using CodeCafe.Application.Common.Uploads;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Ai.Edits;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CodeCafe.Host.Tests;

public sealed class ServerTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Catch singleton-captures-scoped at build time rather than letting it surface as stale reads.
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
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
            services.AddAuthentication(ServerTestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, ServerTestAuthHandler>(
                    ServerTestAuthHandler.SchemeName,
                    _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                if (options.SchemeMap.Remove(
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                    out var existingScheme)
                    && options.Schemes is IList<AuthenticationSchemeBuilder> schemes)
                {
                    _ = schemes.Remove(existingScheme);
                }

                options.AddScheme(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, authScheme =>
                {
                    authScheme.HandlerType = typeof(ServerTestAuthHandler);
                });
                options.DefaultAuthenticateScheme = ServerTestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = ServerTestAuthHandler.SchemeName;
            });

            services.AddSingleton<ServerTestNotebookMutationStore>();
            services.AddSingleton<INotebookMutationStore>(serviceProvider => serviceProvider.GetRequiredService<ServerTestNotebookMutationStore>());
            services.AddSingleton<INotebookReadService, ServerTestNotebookQueryService>();
            services.AddSingleton<INotebookItemMutationService, ServerTestNotebookCommandService>();
            services.AddSingleton<IAuthUserGateway, ServerTestAuthUserGateway>();
            services.AddSingleton<IAuthSessionService, ServerTestAuthSessionService>();
            services.AddSingleton<ServerTestMcpUploadStore>();
            services.AddScoped<IUploadStore>(serviceProvider => serviceProvider.GetRequiredService<ServerTestMcpUploadStore>());
            services.RemoveAll<IAiNotebookEditProposalStore>();
            services.AddSingleton<IAiNotebookEditProposalStore, MemoryAiNotebookEditProposalStore>();
        });
    }
}

public sealed class ServerTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ServerTest";
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
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("scope", "notes.read notes.write")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class ServerTestNotebookQueryService(ServerTestNotebookMutationStore notebookMutationStore) : INotebookReadService
{
    private static readonly Guid NotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(string? search, Guid currentUserId, CancellationToken cancellationToken, int? limit = null, int? offset = null)
        => Task.FromResult<IReadOnlyList<NotebookSummaryModel>>(
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
        ]);

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(Guid currentUserId, string? search, CancellationToken cancellationToken, int? limit = null, int? offset = null)
        => Task.FromResult<IReadOnlyList<NotebookSummaryModel>>(
        currentUserId == OwnerId
            ?
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
                    true,
                    2,
                    1,
                    1,
                    0,
                    false,
                    DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                    DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                    DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                    DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
            ]
            : []);

    public Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(Guid currentUserId, string search, CancellationToken cancellationToken, int? limit = null)
        => Task.FromResult<IReadOnlyList<NotebookItemSearchModel>>(
            notebookMutationStore.GetItems()
                .Where(item =>
                    item.NotebookId == NotebookId
                    && (item.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || item.Path.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || (item.PlainTextContent?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)))
                .Select(item => new NotebookItemSearchModel(
                    NotebookId,
                    "architecture-notes",
                    "Architecture Notes",
                    currentUserId == OwnerId,
                    item.Id,
                    item.Path,
                    item.Title,
                    item.Type,
                    item.PlainTextContent,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc))
                .ToArray());

    public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                && notebookMutationStore.TryGetNotebookDetail(NotebookId, currentUserId, out var notebookDetail)
                ? NotesResult<NotebookDetailModel>.Success(notebookDetail)
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(notebookMutationStore.GetItems())
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookItemModel>.Success(notebookMutationStore.GetItems().Single(item => item.Path == path))
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
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(
                    string.IsNullOrWhiteSpace(search)
                        ? notebookMutationStore.GetItems()
                        : notebookMutationStore.GetItems().Where(item =>
                                item.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || item.Path.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || (item.PlainTextContent?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToArray())
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookItemModel>> GetNotebookItemByIdAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            notebookId == NotebookId
            && notebookMutationStore.GetItems().SingleOrDefault(item => item.Id == itemId) is { } item
                ? NotesResult<NotebookItemModel>.Success(item)
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found."));
}

internal sealed class ServerTestNotebookCommandService(ServerTestNotebookMutationStore notebookMutationStore) : INotebookItemMutationService
{
    public Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(Guid notebookId, Guid currentUserId, Guid? parentId, string type, string title, int sortOrder, JsonElement? contentJson, CancellationToken cancellationToken)
    {
        var item = CreateItem(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            notebookId,
            parentId,
            type,
            title,
            sortOrder,
            contentJson);
        notebookMutationStore.UpsertItem(item);
        return Task.FromResult(NotesResult<NotebookItemModel>.Success(item));
    }

    public Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, string title, JsonElement parentId, int? sortOrder, JsonElement contentJson, CancellationToken cancellationToken, DateTimeOffset? expectedUpdatedAtUtc = null)
    {
        var existingItem = notebookMutationStore.GetItems().SingleOrDefault(item => item.Id == itemId);
        var item = CreateItem(
            itemId,
            notebookId,
            existingItem?.ParentId,
            "page",
            title,
            sortOrder ?? existingItem?.SortOrder ?? 1,
            contentJson.ValueKind is JsonValueKind.Undefined ? null : contentJson);
        notebookMutationStore.UpsertItem(item);
        return Task.FromResult(NotesResult<NotebookItemModel>.Success(item));
    }

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(Guid notebookId, Guid currentUserId, IReadOnlyList<ReorderNotebookItemModel> items, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    public Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult.Success());

    public Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var item = notebookMutationStore.GetItems().SingleOrDefault(existing =>
            existing.Id == itemId && existing.NotebookId == notebookId);
        if (item is null)
        {
            return Task.FromResult(NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."));
        }

        var archived = item with
        {
            IsArchived = true,
            ArchivedAtUtc = DateTimeOffset.UtcNow,
            ArchivedByUserId = currentUserId,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        notebookMutationStore.UpsertItem(archived);
        return Task.FromResult(NotesResult<NotebookItemModel>.Success(archived));
    }

    public Task<NotesResult<NotebookItemModel>> RestoreNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    private static NotebookItemModel CreateItem(
        Guid itemId,
        Guid notebookId,
        Guid? parentId,
        string type,
        string title,
        int sortOrder,
        JsonElement? contentJson)
    {
        var slug = NotebookSlugGenerator.FromTitle(title, type);
        var path = parentId == Guid.Parse("33333333-3333-3333-3333-333333333333")
            ? $"guides/{slug}"
            : slug;

        return new NotebookItemModel(
            itemId,
            notebookId,
            parentId,
            type,
            title,
            slug,
            path,
            sortOrder,
            string.Equals(type, "page", StringComparison.OrdinalIgnoreCase) ? "tiptap_json" : null,
            contentJson,
            string.Equals(type, "page", StringComparison.OrdinalIgnoreCase) ? "Updated content" : null,
            false,
            null,
            null,
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"));
    }
}

internal sealed class ServerTestNotebookMutationStore : INotebookMutationStore
{
    private static readonly Guid DefaultNotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DefaultOwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly IReadOnlyList<NotebookItemModel> DefaultItems =
    [
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DefaultNotebookId,
            null,
            "folder",
            "Guides",
            "guides",
            "guides",
            1,
            null,
            null,
            null,
            false,
            null,
            null,
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00")),
        new(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            DefaultNotebookId,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "page",
            "Overview",
            "overview",
            "guides/overview",
            2,
            "tiptap_json",
            JsonSerializer.SerializeToElement(new
            {
                type = "doc",
                content = new object[]
                {
                    new
                    {
                        type = "paragraph",
                        content = new object[]
                        {
                            new { type = "text", text = "Overview content" }
                        }
                    }
                }
            }),
            "Overview content",
            false,
            null,
            null,
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
    ];
    private readonly Dictionary<Guid, Notebook> _notebooks;
    private readonly List<NotebookItemModel> _items;

    public ServerTestNotebookMutationStore()
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
        _items = DefaultItems.ToList();
    }

    public void AddNotebook(Notebook notebook) => _notebooks[notebook.Id] = notebook;

    public Task<NotebookFavorite?> GetFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult<NotebookFavorite?>(null);

    public Task<Notebook?> GetNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(_notebooks.GetValueOrDefault(notebookId));

    public Task<Notebook?> GetOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(_notebooks.TryGetValue(notebookId, out var notebook) && notebook.OwnerId == currentUserId ? notebook : null);

    public Task<bool> NotebookExistsAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(_notebooks.ContainsKey(notebookId));

    public Task<string> GenerateUniqueNotebookSlugAsync(string title, Guid? currentNotebookId, CancellationToken cancellationToken)
    {
        var baseSlug = string.Join('-', title.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return Task.FromResult(baseSlug);
    }

    public void RemoveNotebook(Notebook notebook) => _notebooks.Remove(notebook.Id);

    public Task SaveNotebookAsync(Notebook notebook, string title, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<int> CountFavoritesAsync(Guid notebookId, CancellationToken cancellationToken) => Task.FromResult(0);

    public Task<bool> IsFavoritedAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task AddFavoriteAsync(NotebookFavorite favorite, CancellationToken cancellationToken) => Task.CompletedTask;

    public void RemoveFavorite(NotebookFavorite favorite)
    {
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public IReadOnlyList<NotebookItemModel> GetItems() => _items;

    public void UpsertItem(NotebookItemModel item)
    {
        var index = _items.FindIndex(existing => existing.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
            return;
        }

        _items.Add(item);
    }

    public bool TryGetNotebookDetail(Guid notebookId, Guid currentUserId, out NotebookDetailModel notebookDetail)
    {
        if (_notebooks.TryGetValue(notebookId, out var notebook))
        {
            var notebookItems = _items
                .Where(item => item.NotebookId == notebookId)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Path, StringComparer.Ordinal)
                .ToArray();
            notebookDetail = new NotebookDetailModel(
                notebook.Id,
                notebook.OwnerId,
                notebook.Title,
                notebook.Slug,
                notebook.Description,
                notebook.Visibility.ToString().ToLowerInvariant(),
                notebook.IsPublished,
                "Yao",
                notebook.OwnerId == currentUserId,
                notebookItems.Length,
                notebookItems.Count(item => string.Equals(item.Type, "folder", StringComparison.OrdinalIgnoreCase)),
                notebookItems.Count(item => string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase)),
                0,
                false,
                notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc,
                notebook.CreatedAtUtc,
                notebook.UpdatedAtUtc,
                notebook.PublishedAtUtc,
                notebookItems);
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
            return TryGetNotebookDetail(notebook.Id, currentUserId, out notebookDetail);
        }

        notebookDetail = null!;
        return false;
    }
}

internal sealed class ServerTestAuthUserGateway : IAuthUserGateway
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

internal sealed class ServerTestAuthSessionService : IAuthSessionService
{
    public Task SignInAsync(Guid userId, bool isPersistent) => Task.CompletedTask;

    public Task SignOutAsync() => Task.CompletedTask;
}

internal sealed class ServerTestMcpUploadStore : IUploadStore
{
    private readonly ConcurrentDictionary<string, UploadState> uploads = new(StringComparer.Ordinal);

    public Task<UploadStatus> CreateAsync(Guid actorId, string? fileName, string mediaType, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var uploadId = Guid.NewGuid().ToString("N");
        var status = new UploadStatus(
            uploadId,
            actorId,
            string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim(),
            0,
            now,
            now);
        uploads[uploadId] = new UploadState(status);
        return Task.FromResult(status);
    }

    public Task<UploadResult<UploadStatus>> CreateTextAsync(
        Guid actorId,
        string? fileName,
        string mediaType,
        string contentText,
        int maxUploadBytes,
        CancellationToken cancellationToken)
    {
        var contentBytes = Encoding.UTF8.GetByteCount(contentText ?? string.Empty);
        if (contentBytes == 0)
        {
            return Task.FromResult(UploadResult<UploadStatus>.Failure("invalid_upload_chunk", "Upload content is required."));
        }

        if (contentBytes > maxUploadBytes)
        {
            return Task.FromResult(UploadResult<UploadStatus>.Failure(
                "upload_too_large",
                $"Upload exceeds the limit of {maxUploadBytes} bytes (received {contentBytes} bytes)."));
        }

        var now = DateTimeOffset.UtcNow;
        var uploadId = Guid.NewGuid().ToString("N");
        var status = new UploadStatus(
            uploadId,
            actorId,
            string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim(),
            contentBytes,
            now,
            now);

        var state = new UploadState(status);
        state.Content.Append(contentText);
        uploads[uploadId] = state;

        return Task.FromResult(UploadResult<UploadStatus>.Success(status));
    }

    public Task<UploadResult<UploadStatus>> AppendTextAsync(
        Guid actorId,
        string uploadId,
        string chunkText,
        int maxChunkBytes,
        int maxUploadBytes,
        CancellationToken cancellationToken)
    {
        if (!uploads.TryGetValue(uploadId, out var state) || state.Status.ActorId != actorId)
        {
            return Task.FromResult(UploadResult<UploadStatus>.Failure("upload_not_found", "Upload session was not found."));
        }

        var chunkBytes = Encoding.UTF8.GetByteCount(chunkText);
        if (chunkBytes == 0)
        {
            return Task.FromResult(UploadResult<UploadStatus>.Failure("invalid_upload_chunk", "Upload chunk text is required."));
        }

        if (chunkBytes > maxChunkBytes)
        {
            return Task.FromResult(UploadResult<UploadStatus>.Failure(
                "upload_chunk_too_large",
                $"Upload chunk exceeds the limit of {maxChunkBytes} bytes (received {chunkBytes} bytes)."));
        }

        lock (state.SyncRoot)
        {
            var nextBytes = state.Status.BytesReceived + chunkBytes;
            if (nextBytes > maxUploadBytes)
            {
                return Task.FromResult(UploadResult<UploadStatus>.Failure(
                    "upload_too_large",
                    $"Upload exceeds the limit of {maxUploadBytes} bytes (received {nextBytes} bytes)."));
            }

            state.Content.Append(chunkText);
            state.Status = state.Status with
            {
                BytesReceived = nextBytes,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            return Task.FromResult(UploadResult<UploadStatus>.Success(state.Status));
        }
    }

    public Task<UploadResult<UploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
    {
        if (!uploads.TryGetValue(uploadId, out var state) || state.Status.ActorId != actorId)
        {
            return Task.FromResult(UploadResult<UploadSession>.Failure("upload_not_found", "Upload session was not found."));
        }

        lock (state.SyncRoot)
        {
            return Task.FromResult(UploadResult<UploadSession>.Success(new UploadSession(
                state.Status.UploadId,
                state.Status.ActorId,
                state.Status.FileName,
                state.Status.MediaType,
                state.Content.ToString(),
                state.Status.BytesReceived,
                state.Status.CreatedAtUtc,
                state.Status.UpdatedAtUtc)));
        }
    }

    public Task<bool> DeleteAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            uploads.TryGetValue(uploadId, out var state)
            && state.Status.ActorId == actorId
            && uploads.TryRemove(uploadId, out _));
    }

    private sealed class UploadState(UploadStatus status)
    {
        public object SyncRoot { get; } = new();

        public StringBuilder Content { get; } = new();

        public UploadStatus Status { get; set; } = status;
    }
}
