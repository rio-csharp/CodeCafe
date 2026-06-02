using CodeCafe.Application.Notes;
using CodeCafe.Mcp.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

namespace CodeCafe.Mcp.Tools.Notes;

[McpServerResourceType]
public sealed class NotesMcpResources
{
    [McpServerResource(
        UriTemplate = "notes://guide",
        Name = "notes_guide",
        Title = "Notes MCP Guide",
        MimeType = "text/markdown")]
    [Description("Recommended MCP workflow for reading, searching, and importing Markdown or TipTap JSON content.")]
    public TextResourceContents GetGuideResource()
    {
        var text = string.Join(Environment.NewLine, new[]
        {
            "# Notes MCP Guide",
            string.Empty,
            "- Use `notebooks://mine`, `notebooks://public`, `notes_list_notebooks`, and `notes_get_notebook` to discover notebooks.",
            $"- Use `{NotesMcpToolNames.ListItems}` to inspect a notebook tree. It supports `parentPath`, `type`, `offset`, `limit`, and `includeArchived` filters. Archived items are owner-only.",
            $"- Use `{NotesMcpToolNames.GetPage}` to inspect a page before editing. It returns `contentJson` as a TipTap JSON string plus `plainTextContent` for quick reading.",
            $"- Use `{NotesMcpToolNames.Search}` to search visible notebooks and page plain-text content.",
            "- For small edits, send inline TipTap JSON directly to page tools.",
            $"- For local files or larger payloads, call `{NotesMcpToolNames.GetLimits}` first, then `{NotesMcpToolNames.CreateUpload}`, append one or more chunks with `{NotesMcpToolNames.AppendUploadChunk}`, and finally import with `{NotesMcpToolNames.CreatePage}`, `{NotesMcpToolNames.UpdatePageContentJson}`, or `{NotesMcpToolNames.AppendBlocksToPage}`.",
            "- Supported uploaded formats are `markdown`, `tiptap_json`, and `tiptap_blocks_json`.",
            "- Uploaded Markdown is converted server-side into TipTap JSON before validation and persistence.",
            $"- Upload sessions are temporary, stored in server memory, and can be discarded with `{NotesMcpToolNames.DiscardUpload}` when no longer needed."
        });

        return new TextResourceContents
        {
            Uri = "notes://guide",
            MimeType = "text/markdown",
            Text = text
        };
    }

    [McpServerResource(
        UriTemplate = "notebooks://mine",
        Name = "my_notebooks",
        Title = "My Notebooks",
        MimeType = "application/json")]
    [Description("Discover notebooks owned by the authenticated actor, including URIs for deeper notebook resources.")]
    public async Task<TextResourceContents> GetMyNotebooksResourceAsync(
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await CreateNotebookDiscoveryResourceAsync(
            "notebooks://mine",
            "mine",
            user,
            notebookReadService,
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
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await CreateNotebookDiscoveryResourceAsync(
            "notebooks://public",
            "public",
            user,
            notebookReadService,
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
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookReadService, cancellationToken));
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
    [Description("Notebook folder and page tree for quick discovery. Use notes_list_items for archive filters, type filters, and pagination.")]
    public async Task<TextResourceContents> GetNotebookItemsResourceAsync(
        string slug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookReadService, cancellationToken));

        var itemsResult = await notebookReadService.GetNotebookItemsAsync(
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
            items.Count,
            0,
            items.Count,
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
    [Description("Page content with the stored TipTap JSON string and derived plain text for inspection before editing.")]
    public async Task<TextResourceContents> GetPageResourceAsync(
        string slug,
        string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookReadService, cancellationToken));
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
        INotebookReadService notebookReadService,
        McpOptions mcpOptions,
        CancellationToken cancellationToken)
    {
        var actorId = NotesMcpSupport.EnsureMcpSuccess(
            NotesMcpSupport.RequireActor(user, mcpOptions.RequiredReadScopes));
        IReadOnlyList<NotebookSummaryModel> notebooks = scope switch
        {
            "mine" => await notebookReadService.GetMyNotebooksAsync(actorId, search: null, cancellationToken, limit: 100),
            "public" => await notebookReadService.GetPublicNotebooksAsync(search: null, actorId, cancellationToken, limit: 100),
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
