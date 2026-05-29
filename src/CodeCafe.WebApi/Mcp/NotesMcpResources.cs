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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes);
        if (!scopeResult.Succeeded)
        {
            throw new InvalidOperationException(scopeResult.Error!.Message);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            throw new InvalidOperationException(notebookResult.Error!.Message);
        }

        var notebook = notebookResult.Value!;
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes);
        if (!scopeResult.Succeeded)
        {
            throw new InvalidOperationException(scopeResult.Error!.Message);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            throw new InvalidOperationException(notebookResult.Error!.Message);
        }

        var itemsResult = await notebookQueryService.GetNotebookItemsAsync(
            notebookResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            null,
            cancellationToken);

        if (!itemsResult.Succeeded)
        {
            throw new InvalidOperationException(itemsResult.Error!.Message);
        }

        var payload = new ListNotebookItemsToolResponse(
            notebookResult.Value!.Id,
            notebookResult.Value!.Slug,
            notebookResult.Value!.Title,
            notebookResult.Value!.CanEdit,
            itemsResult.Value!.Select(item => NotesMcpSupport.ToNotebookItemToolResponse(notebookResult.Value!, item)).ToList());

        return new TextResourceContents
        {
            Uri = $"notebook://{notebookResult.Value!.Slug}/items",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes);
        if (!scopeResult.Succeeded)
        {
            throw new InvalidOperationException(scopeResult.Error!.Message);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            throw new InvalidOperationException(notebookResult.Error!.Message);
        }

        var pageResult = NotesMcpSupport.RequirePage(notebookResult.Value!, path);
        if (!pageResult.Succeeded)
        {
            throw new InvalidOperationException(pageResult.Error!.Message);
        }

        var payload = NotesMcpSupport.ToGetPageToolResponse(notebookResult.Value!, pageResult.Value!);
        return new TextResourceContents
        {
            Uri = $"page://{notebookResult.Value!.Slug}/{pageResult.Value!.Path}",
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
        var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredReadScopes);
        if (!actorResult.Succeeded)
        {
            throw new InvalidOperationException(actorResult.Error!.Message);
        }

        var actorId = actorResult.Value;
        IReadOnlyList<NotebookSummaryModel> notebooks = scope switch
        {
            "mine" => await notebookQueryService.GetMyNotebooksAsync(actorId, search: null, cancellationToken, limit: 25),
            "public" => await notebookQueryService.GetPublicNotebooksAsync(search: null, actorId, cancellationToken, limit: 25),
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
