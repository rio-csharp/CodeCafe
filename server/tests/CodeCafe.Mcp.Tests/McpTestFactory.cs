using CodeCafe.Application.Mcp;
using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Host.Mcp;
using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Host.Common;
using CodeCafe.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CodeCafe.Host.Mcp.Tests;

public sealed class McpTestFactory : WebApplicationFactory<Program>
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
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(McpTestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, McpTestAuthHandler>(
                    McpTestAuthHandler.SchemeName,
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
                    authScheme.HandlerType = typeof(McpTestAuthHandler);
                });
                options.DefaultAuthenticateScheme = McpTestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = McpTestAuthHandler.SchemeName;
            });

            services.AddSingleton<INotebookReadService, TestNotebookQueryService>();
            services.AddSingleton<INotebookItemMutationService, TestNotebookMutationService>();
            services.AddSingleton<IMcpMutationExecutor, TestMcpMutationExecutor>();
            services.AddSingleton<TestMcpUploadStore>();
            services.AddScoped<IMcpUploadStore>(serviceProvider => serviceProvider.GetRequiredService<TestMcpUploadStore>());
            services.AddScoped<IMcpAuditService, NoopMcpAuditService>();
        });
    }
}

internal sealed class McpTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "McpTest";
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

internal sealed class TestNotebookQueryService : INotebookReadService
{
    private static readonly Guid NotebookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly JsonElement OverviewContentJson = JsonSerializer.SerializeToElement(new
    {
        type = "doc",
        content = new[]
        {
            new { type = "paragraph", content = new[] { new { type = "text", text = "First paragraph." } } },
            new { type = "paragraph", content = new[] { new { type = "text", text = "Second paragraph." } } }
        }
    });
    private static readonly NotebookItemModel[] Items =
    [
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            NotebookId,
            null,
            "page",
            "Overview",
            "overview",
            "overview",
            1,
            "tiptap_json",
            OverviewContentJson,
            "Overview content",
            false,
            null,
            null,
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00")),
        new(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            NotebookId,
            null,
            "page",
            "Legacy Overview",
            "legacy-overview",
            "page/legacy-overview",
            2,
            "tiptap_json",
            null,
            "Legacy content",
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
            Items.Length,
            0,
            Items.Length,
            0,
            false,
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            Items);

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
                Items.Length,
                0,
                Items.Length,
                0,
                false,
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
        ]);

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(Guid currentUserId, string? search, CancellationToken cancellationToken, int? limit = null, int? offset = null)
        => Task.FromResult<IReadOnlyList<NotebookSummaryModel>>([]);

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
                ? NotesResult<NotebookDetailModel>.Success(
                    CreateNotebookDetail(currentUserId))
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(Items)
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
            && Items.SingleOrDefault(item => string.Equals(item.Path, NotebookInput.NormalizePath(path), StringComparison.Ordinal)) is { } item
                ? NotesResult<NotebookItemModel>.Success(item)
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
        => Task.FromResult(NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true,
        bool includeContent = true)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookDetailModel>.Success(CreateNotebookDetail(currentUserId))
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

    public Task<NotesResult<NotebookItemsPageModel>> GetNotebookItemsPageAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        Guid? parentId = null,
        string? type = null,
        int? offset = null,
        int? limit = null)
    {
        if (notebookId != NotebookId)
        {
            return Task.FromResult(NotesResult<NotebookItemsPageModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."));
        }

        var filteredItems = Items
            .Where(item => parentId is null || item.ParentId == parentId)
            .Where(item => string.IsNullOrWhiteSpace(type) || string.Equals(item.Type, type, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var pagedItems = filteredItems
            .Skip(Math.Max(0, offset ?? 0))
            .Take(Math.Max(1, limit ?? filteredItems.Count))
            .ToList();
        return Task.FromResult(NotesResult<NotebookItemsPageModel>.Success(
            new NotebookItemsPageModel(filteredItems.Count, pagedItems)));
    }

    public Task<NotesResult<NotebookItemModel>> GetNotebookItemByPathAsync(
        string notebookSlug,
        string path,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
        => Task.FromResult(
            string.Equals(notebookSlug, "architecture-notes", StringComparison.Ordinal)
            && Items.SingleOrDefault(item => string.Equals(item.Path, NotebookInput.NormalizePath(path), StringComparison.Ordinal)) is { } item
                ? NotesResult<NotebookItemModel>.Success(item)
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found."));
}

internal sealed class TestNotebookMutationService : INotebookItemMutationService
{
    public Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(
        Guid notebookId,
        Guid currentUserId,
        Guid? parentId,
        string type,
        string title,
        int sortOrder,
        JsonElement? contentJson,
        CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(CreateItem(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            notebookId,
            parentId,
            type,
            title,
            sortOrder,
            contentJson)));

    public Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        string title,
        JsonElement parentId,
        int? sortOrder,
        JsonElement contentJson,
        CancellationToken cancellationToken,
        DateTimeOffset? expectedUpdatedAtUtc = null)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(CreateItem(
            itemId,
            notebookId,
            null,
            "page",
            title,
            sortOrder ?? 1,
            contentJson.ValueKind is JsonValueKind.Undefined ? null : contentJson)));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        IReadOnlyList<ReorderNotebookItemModel> items,
        CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Success([]));

    public Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult.Success());

    public Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(CreateItem(itemId, notebookId, null, "page", "Archived", 1, null)));

    public Task<NotesResult<NotebookItemModel>> RestoreNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(CreateItem(itemId, notebookId, null, "page", "Restored", 1, null)));

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
        return new NotebookItemModel(
            itemId,
            notebookId,
            parentId,
            type,
            title,
            slug,
            slug,
            sortOrder,
            string.Equals(type, "page", StringComparison.Ordinal) ? "tiptap_json" : null,
            contentJson,
            string.Equals(type, "page", StringComparison.Ordinal) ? "Updated content" : null,
            false,
            null,
            null,
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"));
    }
}

internal sealed class TestMcpUploadStore : IMcpUploadStore
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

internal sealed class TestMcpMutationExecutor : IMcpMutationExecutor
{
    public async Task<CallToolResult> ExecuteAsync<T>(
        ClaimsPrincipal user,
        string toolName,
        Func<CancellationToken, Task<McpMutationResult<T>>> operation,
        CancellationToken cancellationToken)
        where T : class
    {
        var result = await operation(cancellationToken);
        return result.Succeeded
            ? NotesMcpResultMapper.Success(result.Value!, result.SuccessText!)
            : NotesMcpResultMapper.Failure(result.Error!);
    }
}

internal sealed class NoopMcpAuditService : IMcpAuditService
{
    public Task WriteAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task WriteIndependentAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken) => Task.CompletedTask;
}
