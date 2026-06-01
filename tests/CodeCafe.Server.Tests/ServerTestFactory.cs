using CodeCafe.Api.Endpoints.Auth;
using CodeCafe.Application.Notes;
using CodeCafe.Server.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CodeCafe.Server.Tests;

public sealed class ServerTestFactory : WebApplicationFactory<ServerAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(ServerTestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, ServerTestAuthHandler>(
                    ServerTestAuthHandler.SchemeName,
                    _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = ServerTestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = ServerTestAuthHandler.SchemeName;
            });

            services.AddSingleton<INotebookQueryService, ServerTestNotebookQueryService>();
            services.AddSingleton<INotebookCommandService, ServerTestNotebookCommandService>();
            services.AddSingleton<INotebookFavoriteService, ServerTestNotebookFavoriteService>();
            services.AddSingleton<IAuthEndpointService, ServerTestAuthEndpointService>();
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

internal sealed class ServerTestNotebookQueryService : INotebookQueryService
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

    public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
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

    public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            notebookId == NotebookId
                ? NotesResult<NotebookDetailModel>.Success(CreateNotebookDetail(currentUserId))
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookDetailModel>.Success(CreateNotebookDetail(currentUserId))
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

internal sealed class ServerTestNotebookCommandService : INotebookCommandService
{
    public Task<NotesResult<NotebookDetailModel>> CreateNotebookAsync(Guid currentUserId, string title, string? description, string? visibility, CancellationToken cancellationToken)
    {
        var result = new NotebookDetailModel(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            currentUserId,
            title,
            "combined-host-notebook",
            description,
            visibility ?? "private",
            string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase),
            "Yao",
            true,
            0,
            0,
            0,
            0,
            false,
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            null,
            []);

        return Task.FromResult(NotesResult<NotebookDetailModel>.Success(result));
    }

    public Task<NotesResult<NotebookDetailModel>> UpdateNotebookAsync(Guid notebookId, Guid currentUserId, string title, string? description, string? visibility, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    public Task<NotesResult> DeleteNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult.Success());

    public Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(Guid notebookId, Guid currentUserId, Guid? parentId, string type, string title, int sortOrder, JsonElement? contentJson, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    public Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, string title, JsonElement parentId, int? sortOrder, JsonElement contentJson, CancellationToken cancellationToken, DateTimeOffset? expectedUpdatedAtUtc = null)
        => Task.FromResult(NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(Guid notebookId, Guid currentUserId, IReadOnlyList<ReorderNotebookItemModel> items, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    public Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult.Success());

    public Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));

    public Task<NotesResult<NotebookItemModel>> RestoreNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "not_implemented", "Not implemented in server tests."));
}

internal sealed class ServerTestNotebookFavoriteService : INotebookFavoriteService
{
    public Task<NotesResult<NotebookFavoriteModel>> GetFavoriteStatusAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookFavoriteModel>.Success(new NotebookFavoriteModel(notebookId, false, 0)));

    public Task<NotesResult<NotebookFavoriteModel>> AddFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookFavoriteModel>.Success(new NotebookFavoriteModel(notebookId, true, 1)));

    public Task<NotesResult<NotebookFavoriteModel>> RemoveFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookFavoriteModel>.Success(new NotebookFavoriteModel(notebookId, false, 0)));
}

internal sealed class ServerTestAuthEndpointService : IAuthEndpointService
{
    public Task<AuthOperationResult<AuthResponse>> RegisterAsync(RegisterRequest request, HttpContext httpContext)
        => Task.FromResult(AuthOperationResult<AuthResponse>.Success(
            StatusCodes.Status200OK,
            new AuthResponse(new UserResponse(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                request.Email.Trim().ToLowerInvariant(),
                request.DisplayName.Trim()))));

    public Task<AuthOperationResult<AuthResponse>> LoginAsync(LoginRequest request, HttpContext httpContext)
        => Task.FromResult(AuthOperationResult<AuthResponse>.Success(
            StatusCodes.Status200OK,
            new AuthResponse(new UserResponse(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                request.Email.Trim().ToLowerInvariant(),
                "Yao"))));

    public Task<AuthOperationResult<AuthResponse>> MeAsync(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Task.FromResult(subject is null
            ? AuthOperationResult<AuthResponse>.Failure(
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                "Authentication is required.")
            : AuthOperationResult<AuthResponse>.Success(
                StatusCodes.Status200OK,
                new AuthResponse(new UserResponse(
                    Guid.Parse(subject),
                    "yao@example.com",
                    "Yao"))));
    }

    public Task<LogoutResponse> LogoutAsync()
        => Task.FromResult(new LogoutResponse(true));
}
