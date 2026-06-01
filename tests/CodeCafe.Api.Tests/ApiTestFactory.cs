using System.Security.Claims;
using System.Text.Encodings.Web;
using CodeCafe.Application.Notes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCafe.Api.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
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

            services.AddSingleton<INotebookQueryService, TestNotebookQueryService>();
            services.AddSingleton<INotebookCommandService, TestNotebookCommandService>();
            services.AddSingleton<INotebookFavoriteService, TestNotebookFavoriteService>();
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

internal sealed class TestNotebookQueryService : INotebookQueryService
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

    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(string? search, Guid currentUserId, CancellationToken cancellationToken, int? limit = null)
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

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(Guid currentUserId, string? search, CancellationToken cancellationToken, int? limit = null)
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

    public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookDetailModel>.Success(PublicNotebook)
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

    public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            notebookId == NotebookId
                ? NotesResult<NotebookDetailModel>.Success(PublicNotebook with
                {
                    OwnerId = currentUserId,
                    CanEdit = true
                })
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => Task.FromResult(
            string.Equals(slug, "architecture-notes", StringComparison.Ordinal)
                ? NotesResult<NotebookDetailModel>.Success(PublicNotebook with
                {
                    CanEdit = currentUserId == OwnerId
                })
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(Guid notebookId, Guid currentUserId, string? search, CancellationToken cancellationToken, bool includeArchived = false, int? limit = null)
        => Task.FromResult(
            notebookId == NotebookId
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Success(Items)
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found."));
}

internal sealed class TestNotebookCommandService : INotebookCommandService
{
    private static readonly Guid DefaultItemId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public Task<NotesResult<NotebookDetailModel>> CreateNotebookAsync(Guid currentUserId, string title, string? description, string? visibility, CancellationToken cancellationToken)
    {
        var result = new NotebookDetailModel(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            currentUserId,
            title,
            "new-notebook",
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
            string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase)
                ? DateTimeOffset.Parse("2026-06-01T00:00:00+00:00")
                : null,
            []);

        return Task.FromResult(NotesResult<NotebookDetailModel>.Success(result));
    }

    public Task<NotesResult<NotebookDetailModel>> UpdateNotebookAsync(Guid notebookId, Guid currentUserId, string title, string? description, string? visibility, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookDetailModel>.Success(
            new NotebookDetailModel(
                notebookId,
                currentUserId,
                title,
                "updated-notebook",
                description,
                visibility ?? "private",
                string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase),
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
                string.Equals(visibility, "public", StringComparison.OrdinalIgnoreCase)
                    ? DateTimeOffset.Parse("2026-06-01T00:00:00+00:00")
                    : null,
                [])));

    public Task<NotesResult> DeleteNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult.Success());

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

internal sealed class TestNotebookFavoriteService : INotebookFavoriteService
{
    public Task<NotesResult<NotebookFavoriteModel>> GetFavoriteStatusAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookFavoriteModel>.Success(
            new NotebookFavoriteModel(notebookId, false, 1)));

    public Task<NotesResult<NotebookFavoriteModel>> AddFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookFavoriteModel>.Success(
            new NotebookFavoriteModel(notebookId, true, 2)));

    public Task<NotesResult<NotebookFavoriteModel>> RemoveFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(NotesResult<NotebookFavoriteModel>.Success(
            new NotebookFavoriteModel(notebookId, false, 1)));
}
