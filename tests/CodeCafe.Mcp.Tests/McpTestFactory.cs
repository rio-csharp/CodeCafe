using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Application.Notes;
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

namespace CodeCafe.Mcp.Tests;

public sealed class McpTestFactory : WebApplicationFactory<ServerAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
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
            null,
            "Overview content",
            false,
            null,
            null,
            DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
    ];

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
                1,
                0,
                1,
                0,
                false,
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"))
        ]);

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(Guid currentUserId, string? search, CancellationToken cancellationToken, int? limit = null)
        => Task.FromResult<IReadOnlyList<NotebookSummaryModel>>([]);

    public Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(Guid currentUserId, string search, CancellationToken cancellationToken, int? limit = null)
        => Task.FromResult<IReadOnlyList<NotebookItemSearchModel>>([]);

    public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookDetailModel>.Success(
                    new NotebookDetailModel(
                        NotebookId,
                        OwnerId,
                        "Architecture Notes",
                        "architecture-notes",
                        "Refactor plan",
                        "public",
                        true,
                        "Yao",
                        false,
                        1,
                        0,
                        1,
                        0,
                        false,
                        DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                        DateTimeOffset.Parse("2026-05-31T00:00:00+00:00"),
                        DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                        DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
                        Items))
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Success(Items));

    public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Success(Items[0]));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(Guid notebookId, Guid currentUserId, string? search, CancellationToken cancellationToken, bool includeArchived = false, int? limit = null)
        => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));
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

internal sealed class NoopMcpAuditService : IMcpAuditService
{
    public Task WriteAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task WriteIndependentAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken) => Task.CompletedTask;
}
