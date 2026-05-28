using CodeCafe.Application.Notes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

namespace CodeCafe.WebApi.Mcp;

[McpServerPromptType]
public sealed class NotesMcpPrompts
{
    [McpServerPrompt(Name = "notes.summarize_page", Title = "Summarize Page")]
    [Description("Guide a client to summarize a notebook page with notes_get_page.")]
    public async Task<IEnumerable<ChatMessage>> SummarizePageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
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

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            throw new InvalidOperationException(notebookResult.Error!.Message);
        }

        var pageResult = NotesMcpSupport.RequirePage(notebookResult.Value!, path);
        if (!pageResult.Succeeded)
        {
            throw new InvalidOperationException(pageResult.Error!.Message);
        }

        return NotesMcpSupport.CreatePromptMessages(
            $"Use `{NotesMcpToolNames.GetPage}` for notebook `{notebookResult.Value!.Slug}` and path `{pageResult.Value!.Path}`.",
            "Summarize the page, preserve technical accuracy, and propose a tighter heading structure if the content suggests one.");
    }

    [McpServerPrompt(Name = "notes.organize_notebook", Title = "Organize Notebook")]
    [Description("Guide a client to inspect a notebook and propose a folder/page reorganization plan.")]
    public async Task<IEnumerable<ChatMessage>> OrganizeNotebookAsync(
        [Description("The notebook slug.")] string notebookSlug,
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

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            throw new InvalidOperationException(notebookResult.Error!.Message);
        }

        return NotesMcpSupport.CreatePromptMessages(
            $"Inspect notebook `{notebookResult.Value!.Slug}` with `{NotesMcpToolNames.GetNotebook}` and `{NotesMcpToolNames.ListItems}`.",
            $"Propose a reorganization plan first. Only after review should `{NotesMcpToolNames.MoveItem}` or `{NotesMcpToolNames.ReorderItems}` be used.");
    }

    [McpServerPrompt(Name = "notes.expand_outline", Title = "Expand Outline")]
    [Description("Guide a client to expand an existing page outline into a fuller draft.")]
    public async Task<IEnumerable<ChatMessage>> ExpandOutlineAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
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

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            throw new InvalidOperationException(notebookResult.Error!.Message);
        }

        var pageResult = NotesMcpSupport.RequirePage(notebookResult.Value!, path);
        if (!pageResult.Succeeded)
        {
            throw new InvalidOperationException(pageResult.Error!.Message);
        }

        return NotesMcpSupport.CreatePromptMessages(
            $"Read page `{pageResult.Value!.Path}` in notebook `{notebookResult.Value!.Slug}` with `{NotesMcpToolNames.GetPage}`.",
            $"Expand the outline into a fuller draft while preserving the existing structure. When applying edits, use `{NotesMcpToolNames.UpdatePageContentJson}` or `{NotesMcpToolNames.AppendBlocksToPage}`.");
    }

    [McpServerPrompt(Name = "notes.review_for_staleness", Title = "Review For Staleness")]
    [Description("Guide a client to identify stale pages in a notebook.")]
    public async Task<IEnumerable<ChatMessage>> ReviewForStalenessAsync(
        [Description("The notebook slug.")] string notebookSlug,
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

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            throw new InvalidOperationException(notebookResult.Error!.Message);
        }

        return NotesMcpSupport.CreatePromptMessages(
            $"Inspect notebook `{notebookResult.Value!.Slug}` with `{NotesMcpToolNames.GetNotebook}`, `{NotesMcpToolNames.ListItems}`, and `{NotesMcpToolNames.GetPage}` as needed.",
            "Identify pages that appear stale based on timestamps, structure, and content drift. Produce findings before proposing edits.");
    }
}
