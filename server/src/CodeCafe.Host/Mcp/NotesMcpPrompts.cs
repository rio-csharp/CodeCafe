using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Configuration;
using CodeCafe.Application.Common.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

namespace CodeCafe.Host.Mcp;

[McpServerPromptType]
public sealed class NotesMcpPrompts
{
    [McpServerPrompt(Name = "notes.summarize_page", Title = "Summarize Page")]
    [Description("Guide a client to summarize a notebook page after fetching its TipTap JSON and plain text with notes_get_page.")]
    public async Task<IEnumerable<ChatMessage>> SummarizePageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookReadService, cancellationToken));
        var page = NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequirePage(
            NotesMcpSupport.EnsureMcpSuccess(await NotesMcpSupport.GetNotebookItemByMcpPathAsync(
                notebook.Slug,
                path,
                CurrentUserClaims.GetUserId(user) ?? Guid.Empty,
                notebookReadService,
                cancellationToken,
                itemType: "page"))));

        return NotesMcpSupport.CreatePromptMessages(
            $"Use `{NotesMcpToolNames.GetPage}` for notebook `{notebook.Slug}` and path `{page.Path}`.",
            "Summarize the page, preserve technical accuracy, and propose a tighter heading structure if the content suggests one.");
    }

    [McpServerPrompt(Name = "notes.organize_notebook", Title = "Organize Notebook")]
    [Description("Guide a client to inspect a notebook and propose a folder/page reorganization plan.")]
    public async Task<IEnumerable<ChatMessage>> OrganizeNotebookAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookReadService, cancellationToken));

        return NotesMcpSupport.CreatePromptMessages(
            $"Inspect notebook `{notebook.Slug}` with `{NotesMcpToolNames.GetNotebook}` and `{NotesMcpToolNames.ListItems}`.",
            $"Propose a reorganization plan first. Only after review should `{NotesMcpToolNames.MoveItem}` or `{NotesMcpToolNames.ReorderItems}` be used.");
    }

    [McpServerPrompt(Name = "notes.expand_outline", Title = "Expand Outline")]
    [Description("Guide a client to expand an existing page outline into a fuller draft.")]
    public async Task<IEnumerable<ChatMessage>> ExpandOutlineAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookReadService, cancellationToken));
        var page = NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequirePage(
            NotesMcpSupport.EnsureMcpSuccess(await NotesMcpSupport.GetNotebookItemByMcpPathAsync(
                notebook.Slug,
                path,
                CurrentUserClaims.GetUserId(user) ?? Guid.Empty,
                notebookReadService,
                cancellationToken,
                itemType: "page"))));

        return NotesMcpSupport.CreatePromptMessages(
            $"Read page `{page.Path}` in notebook `{notebook.Slug}` with `{NotesMcpToolNames.GetPage}`.",
            $"Expand the outline into a fuller draft while preserving the existing structure. The editor supports headings, paragraphs, lists (bullet/ordered/task), code blocks, blockquotes, tables, images, YouTube embeds, and inline formatting (bold, italic, underline, strikethrough, links, colors, highlights, subscript, superscript, font family). Use inline TipTap JSON only for smaller edits. For larger drafts or local Markdown files, call `{NotesMcpToolNames.PrepareHttpUpload}` first and use the returned HTTP upload plan so the Markdown file can be uploaded directly with the same bearer token. After upload, apply the returned upload id with `{NotesMcpToolNames.UpdatePageContent}` or `{NotesMcpToolNames.AppendBlocksToPage}` using `markdown` format. Only if direct HTTP upload is unavailable should you fall back to `{NotesMcpToolNames.GetLimits}`, `{NotesMcpToolNames.CreateUpload}`, and `{NotesMcpToolNames.AppendUploadChunk}`.");
    }

    [McpServerPrompt(Name = "notes.review_for_staleness", Title = "Review For Staleness")]
    [Description("Guide a client to identify stale pages in a notebook.")]
    public async Task<IEnumerable<ChatMessage>> ReviewForStalenessAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        NotesMcpSupport.EnsureMcpSuccess(NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes));
        var notebook = NotesMcpSupport.EnsureMcpSuccess(
            await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookReadService, cancellationToken));

        return NotesMcpSupport.CreatePromptMessages(
            $"Inspect notebook `{notebook.Slug}` with `{NotesMcpToolNames.GetNotebook}`, `{NotesMcpToolNames.ListItems}`, and `{NotesMcpToolNames.GetPage}` as needed.",
            $"Identify pages that appear stale based on timestamps, structure, and content drift. Produce findings before proposing edits. If archived items matter, remember `{NotesMcpToolNames.ListItems}` with `includeArchived=true` is only available to the notebook owner.");
    }
}
