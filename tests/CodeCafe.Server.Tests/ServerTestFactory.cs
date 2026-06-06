using CodeCafe.Api.Endpoints.Auth;
using CodeCafe.Application.Auth;
using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Mcp.Tools.Notes;
using CodeCafe.Server.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CodeCafe.Server.Tests;

public sealed class ServerTestFactory : WebApplicationFactory<ServerAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
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
            services.AddScoped<IMcpUploadStore>(serviceProvider => serviceProvider.GetRequiredService<ServerTestMcpUploadStore>());
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
    private static readonly NotebookItemModel[] Items =
    [
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            NotebookId,
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
            NotebookId,
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

    private static NotebookDetailModel CreateNotebookDetail(Guid currentUserId)
        => new(
            NotebookId,
            OwnerId,
            "Architecture Notes",
            "architecture-notes",
            "Refactor plan",
            "public",
            true,
            "Yao",
            currentUserId == OwnerId,
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

    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(string? search, Guid currentUserId, CancellationToken cancellationToken, int? limit = null)
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

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(Guid currentUserId, string? search, CancellationToken cancellationToken, int? limit = null)
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
        search.Contains("overview", StringComparison.OrdinalIgnoreCase)
            ?
            [
                new(
                    NotebookId,
                    "architecture-notes",
                    "Architecture Notes",
                    currentUserId == OwnerId,
                    Items[1].Id,
                    Items[1].Path,
                    Items[1].Title,
                    Items[1].Type,
                    Items[1].PlainTextContent,
                    Items[1].CreatedAtUtc,
                    Items[1].UpdatedAtUtc)
            ]
            : []);

    public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookDetailModel>.Success(CreateNotebookDetail(currentUserId))
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(Items)
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookItemModel>.Success(Items[1])
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true)
        => Task.FromResult(
            notebookMutationStore.TryGetNotebookDetail(notebookId, currentUserId, out var notebookDetail)
                ? NotesResult<NotebookDetailModel>.Success(notebookDetail)
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true)
        => Task.FromResult(
            notebookMutationStore.TryGetNotebookDetailBySlug(slug, currentUserId, out var notebookDetail)
                ? NotesResult<NotebookDetailModel>.Success(notebookDetail)
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(Guid notebookId, Guid currentUserId, string? search, CancellationToken cancellationToken, bool includeArchived = false, int? limit = null)
        => Task.FromResult(
            notebookId == NotebookId
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(
                    string.IsNullOrWhiteSpace(search)
                        ? Items
                        : Items.Where(item =>
                                item.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || item.Path.Contains(search, StringComparison.OrdinalIgnoreCase)
                                || (item.PlainTextContent?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToArray())
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));
}

internal sealed class ServerTestNotebookCommandService : INotebookItemMutationService
{
    public Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(Guid notebookId, Guid currentUserId, Guid? parentId, string type, string title, int sortOrder, JsonElement? contentJson, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(CreateItem(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            notebookId,
            parentId,
            type,
            title,
            sortOrder,
            contentJson)));

    public Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, string title, JsonElement parentId, int? sortOrder, JsonElement contentJson, CancellationToken cancellationToken, DateTimeOffset? expectedUpdatedAtUtc = null)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(CreateItem(
            itemId,
            notebookId,
            null,
            "page",
            title,
            sortOrder ?? 1,
            contentJson.ValueKind is JsonValueKind.Undefined ? null : contentJson)));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(Guid notebookId, Guid currentUserId, IReadOnlyList<ReorderNotebookItemModel> items, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    public Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult.Success());

    public Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

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

    public void AddFavorite(NotebookFavorite favorite)
    {
    }

    public void RemoveFavorite(NotebookFavorite favorite)
    {
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public bool TryGetNotebookDetail(Guid notebookId, Guid currentUserId, out NotebookDetailModel notebookDetail)
    {
        if (_notebooks.TryGetValue(notebookId, out var notebook))
        {
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
                DefaultItems.Count,
                1,
                1,
                0,
                false,
                notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc,
                notebook.CreatedAtUtc,
                notebook.UpdatedAtUtc,
                notebook.PublishedAtUtc,
                DefaultItems);
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

    public Task<AuthPasswordVerificationResult> VerifyPasswordAsync(
        Guid userId,
        string password,
        bool lockoutOnFailure,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            password == "Password123!"
                ? AuthPasswordVerificationResult.Success()
                : AuthPasswordVerificationResult.Failure(isLockedOut: false));
    }
}

internal sealed class ServerTestAuthSessionService : IAuthSessionService
{
    public Task SignInAsync(Guid userId, bool isPersistent) => Task.CompletedTask;

    public Task SignOutAsync() => Task.CompletedTask;
}

internal sealed class ServerTestMcpUploadStore : IMcpUploadStore
{
    private readonly ConcurrentDictionary<string, UploadState> uploads = new(StringComparer.Ordinal);

    public Task<McpUploadStatus> CreateAsync(Guid actorId, string? fileName, string mediaType, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var uploadId = Guid.NewGuid().ToString("N");
        var status = new McpUploadStatus(
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

    public Task<NotesUploadResult<McpUploadStatus>> CreateTextAsync(
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
            return Task.FromResult(NotesUploadResult<McpUploadStatus>.Failure("invalid_upload_chunk", "Upload content is required."));
        }

        if (contentBytes > maxUploadBytes)
        {
            return Task.FromResult(NotesUploadResult<McpUploadStatus>.Failure(
                "upload_too_large",
                $"Upload exceeds the limit of {maxUploadBytes} bytes (received {contentBytes} bytes)."));
        }

        var now = DateTimeOffset.UtcNow;
        var uploadId = Guid.NewGuid().ToString("N");
        var status = new McpUploadStatus(
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

        return Task.FromResult(NotesUploadResult<McpUploadStatus>.Success(status));
    }

    public Task<NotesUploadResult<McpUploadStatus>> AppendTextAsync(
        Guid actorId,
        string uploadId,
        string chunkText,
        int maxChunkBytes,
        int maxUploadBytes,
        CancellationToken cancellationToken)
    {
        if (!uploads.TryGetValue(uploadId, out var state) || state.Status.ActorId != actorId)
        {
            return Task.FromResult(NotesUploadResult<McpUploadStatus>.Failure("upload_not_found", "Upload session was not found."));
        }

        var chunkBytes = Encoding.UTF8.GetByteCount(chunkText);
        if (chunkBytes == 0)
        {
            return Task.FromResult(NotesUploadResult<McpUploadStatus>.Failure("invalid_upload_chunk", "Upload chunk text is required."));
        }

        if (chunkBytes > maxChunkBytes)
        {
            return Task.FromResult(NotesUploadResult<McpUploadStatus>.Failure(
                "upload_chunk_too_large",
                $"Upload chunk exceeds the limit of {maxChunkBytes} bytes (received {chunkBytes} bytes)."));
        }

        lock (state.SyncRoot)
        {
            var nextBytes = state.Status.BytesReceived + chunkBytes;
            if (nextBytes > maxUploadBytes)
            {
                return Task.FromResult(NotesUploadResult<McpUploadStatus>.Failure(
                    "upload_too_large",
                    $"Upload exceeds the limit of {maxUploadBytes} bytes (received {nextBytes} bytes)."));
            }

            state.Content.Append(chunkText);
            state.Status = state.Status with
            {
                BytesReceived = nextBytes,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            return Task.FromResult(NotesUploadResult<McpUploadStatus>.Success(state.Status));
        }
    }

    public Task<NotesUploadResult<McpUploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
    {
        if (!uploads.TryGetValue(uploadId, out var state) || state.Status.ActorId != actorId)
        {
            return Task.FromResult(NotesUploadResult<McpUploadSession>.Failure("upload_not_found", "Upload session was not found."));
        }

        lock (state.SyncRoot)
        {
            return Task.FromResult(NotesUploadResult<McpUploadSession>.Success(new McpUploadSession(
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

    private sealed class UploadState(McpUploadStatus status)
    {
        public object SyncRoot { get; } = new();

        public StringBuilder Content { get; } = new();

        public McpUploadStatus Status { get; set; } = status;
    }
}
