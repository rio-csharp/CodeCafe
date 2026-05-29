using CodeCafe.Application.Notes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace CodeCafe.WebApi.Mcp;

[McpServerResourceType]
public sealed class NotesMcpResources
{
    [McpServerResource(
        UriTemplate = "notebooks://mine",
        Name = "my_notebooks",
        Title = "My Notebooks",
        MimeType = "application/json")]
    [Description("Discover notebooks owned by the authenticated actor, including URIs for deeper notebook resources.")]
    public async Task<TextResourceContents> GetMyNotebooksResourceAsync(
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await CreateNotebookDiscoveryResourceAsync(
            "notebooks://mine",
            "mine",
            user,
            notebookQueryService,
            mcpOptionsAccessor.Value,
            cancellationToken);
    }

    [McpServerResource(
        UriTemplate = "notebooks://public",
        Name = "public_notebooks",
        Title = "Public Notebooks",
        MimeType = "application/json")]
    [Description("Discover public notebooks visible to the authenticated actor, including URIs for deeper notebook resources.")]
    public async Task<TextResourceContents> GetPublicNotebooksResourceAsync(
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await CreateNotebookDiscoveryResourceAsync(
            "notebooks://public",
            "public",
            user,
            notebookQueryService,
            mcpOptionsAccessor.Value,
            cancellationToken);
    }

    [McpServerResource(
        UriTemplate = "notebook://{slug}",
        Name = "notebook",
        Title = "Notebook",
        MimeType = "application/json")]
    [Description("Notebook metadata and summary.")]
    public async Task<TextResourceContents> GetNotebookResourceAsync(
        string slug,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookQueryService, cancellationToken));
        var payload = NotesMcpSupport.ToGetNotebookToolResponse(notebook);

        return new TextResourceContents
        {
            Uri = $"notebook://{notebook.Slug}",
            MimeType = "application/json",
            Text = NotesMcpSupport.SerializeToJson(payload)
        };
    }

    [McpServerResource(
        UriTemplate = "notebook://{slug}/items",
        Name = "notebook_items",
        Title = "Notebook Items",
        MimeType = "application/json")]
    [Description("Notebook folder and page tree.")]
    public async Task<TextResourceContents> GetNotebookItemsResourceAsync(
        string slug,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookQueryService, cancellationToken));

        var itemsResult = await notebookQueryService.GetNotebookItemsAsync(
            notebook.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            null,
            cancellationToken);
        var items = NotesMcpSupport.EnsureMcpSuccess(itemsResult);

        var payload = new ListNotebookItemsToolResponse(
            notebook.Id,
            notebook.Slug,
            notebook.Title,
            notebook.CanEdit,
            items.Select(item => NotesMcpSupport.ToNotebookItemToolResponse(notebook, item)).ToList());

        return new TextResourceContents
        {
            Uri = $"notebook://{notebook.Slug}/items",
            MimeType = "application/json",
            Text = NotesMcpSupport.SerializeToJson(payload)
        };
    }

    [McpServerResource(
        UriTemplate = "page://{slug}/{path}",
        Name = "page",
        Title = "Page",
        MimeType = "application/json")]
    [Description("Page TipTap JSON and derived plain text.")]
    public async Task<TextResourceContents> GetPageResourceAsync(
        string slug,
        string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookQueryService, cancellationToken));
        var page = NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequirePage(notebook, path));
        var payload = NotesMcpSupport.ToGetPageToolResponse(notebook, page);
        return new TextResourceContents
        {
            Uri = $"page://{notebook.Slug}/{page.Path}",
            MimeType = "application/json",
            Text = NotesMcpSupport.SerializeToJson(payload)
        };
    }

    private static async Task<TextResourceContents> CreateNotebookDiscoveryResourceAsync(
        string resourceUri,
        string scope,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        McpOptions mcpOptions,
        CancellationToken cancellationToken)
    {
        var actorId = NotesMcpSupport.EnsureMcpSuccess(
            NotesMcpSupport.RequireActor(user, mcpOptions.RequiredReadScopes));
        IReadOnlyList<NotebookSummaryModel> notebooks = scope switch
        {
            "mine" => await notebookQueryService.GetMyNotebooksAsync(actorId, search: null, cancellationToken, limit: 100),
            "public" => await notebookQueryService.GetPublicNotebooksAsync(search: null, actorId, cancellationToken, limit: 100),
            _ => []
        };

        var payload = new NotebookDiscoveryResourceResponse(
            scope,
            notebooks.Count,
            notebooks.Select(notebook => new NotebookDiscoveryItem(
                notebook.Title,
                notebook.Slug,
                notebook.Visibility,
                notebook.CanEdit,
                notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc,
                $"notebook://{notebook.Slug}",
                $"notebook://{notebook.Slug}/items"))
            .ToList());

        return new TextResourceContents
        {
            Uri = resourceUri,
            MimeType = "application/json",
            Text = NotesMcpSupport.SerializeToJson(payload)
        };
    }

    private sealed record NotebookDiscoveryResourceResponse(
        string Scope,
        int TotalCount,
        IReadOnlyList<NotebookDiscoveryItem> Notebooks);

    private sealed record NotebookDiscoveryItem(
        string Title,
        string Slug,
        string Visibility,
        bool CanEdit,
        DateTimeOffset LastUpdatedAtUtc,
        string NotebookUri,
        string ItemsUri);
}
