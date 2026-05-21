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
        var payload = new GetNotebookToolResponse(
            notebook.Id,
            notebook.OwnerId,
            notebook.Slug,
            notebook.Title,
            notebook.Description,
            notebook.Visibility,
            notebook.IsPublished,
            notebook.AuthorDisplayName,
            notebook.CanEdit,
            notebook.ItemCount,
            notebook.FolderCount,
            notebook.PageCount,
            notebook.FavoriteCount,
            notebook.IsFavoritedByMe,
            notebook.LastActivityAtUtc,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc,
            notebook.PublishedAtUtc);

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
            itemsResult.Value!.Select(NotesMcpSupport.ToNotebookItemToolResponse).ToList());

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
}
