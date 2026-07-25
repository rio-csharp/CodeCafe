using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebook;
using CodeCafe.Modules.Notes.Application.Notes.Commands.DeleteNotebook;
using CodeCafe.Modules.Notes.Application.Notes.Commands.UpdateNotebook;
using CodeCafe.Shared.Application.Configuration;
using MediatR;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

[McpServerToolType]
public sealed class NotesMcpNotebookTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.ListNotebooks,
        Title = "List Notebooks",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ListNotebooksToolResponse))]
    [Description("List notebooks visible to the authenticated actor.")]
    public async Task<CallToolResult> ListNotebooksAsync(
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        IOptions<McpOptions> mcpOptionsAccessor,
        [Description("Notebook scope: all, mine, or public.")] string? scope = null,
        [Description("Optional search query to filter notebooks by title or description.")] string? query = null,
        [Description("Maximum number of notebooks to return.")] int? limit = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredReadScopes);
        if (!actorResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(actorResult.Error!);
        }

        var currentUserId = actorResult.Value;
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();
        if (normalizedScope is not ("all" or "mine" or "public"))
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_scope",
                "Scope must be all, mine, or public."));
        }

        var maxResults = Math.Clamp(limit ?? 25, 1, mcpOptions.MaxListItemsLimit);
        var notebooks = new List<NotebookSummaryModel>();

        if (normalizedScope is "all" or "mine")
        {
            notebooks.AddRange(await notebookReadService.GetMyNotebooksAsync(currentUserId, query, cancellationToken, maxResults));
        }

        if (normalizedScope is "all" or "public")
        {
            notebooks.AddRange(await notebookReadService.GetPublicNotebooksAsync(query, currentUserId, cancellationToken, maxResults));
        }

        var notebookDetails = notebooks
            .GroupBy(notebook => notebook.Id)
            .Select(group => group.First())
            .OrderByDescending(notebook => notebook.LastActivityAtUtc)
            .ThenBy(notebook => notebook.Title, StringComparer.OrdinalIgnoreCase)
            .Select(NotesMcpSupport.ToGetNotebookToolResponse)
            .Take(maxResults)
            .ToList();

        var response = new ListNotebooksToolResponse(normalizedScope, notebookDetails.Count, notebookDetails);
        return NotesMcpResultMapper.Success(response, $"Listed {response.TotalCount} notebook(s) for scope '{response.Scope}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.GetNotebook,
        Title = "Get Notebook",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(GetNotebookToolResponse))]
    [Description("Read notebook metadata for a notebook the authenticated actor is allowed to access.")]
    public async Task<CallToolResult> GetNotebookAsync(
        [Description("The notebook slug.")] string slug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var notebookContextResult = await NotesMcpSupport.RequireNotebookSummaryContextAsync(
            slug,
            user,
            notebookReadService,
            cancellationToken,
            mcpOptions.RequiredReadScopes);
        if (!notebookContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookContextResult.Error!);
        }

        var response = NotesMcpSupport.ToGetNotebookToolResponse(notebookContextResult.Value.Notebook);
        return NotesMcpResultMapper.Success(response, $"Notebook '{response.Title}' loaded.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.GetLimits,
        Title = "Get MCP Limits",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(GetNotesLimitsToolResponse))]
    [Description("Return inline, upload, page-size, and pagination limits. Call this before sending large content.")]
    public CallToolResult GetLimits(
        IOptions<McpOptions> mcpOptionsAccessor)
    {
        var options = mcpOptionsAccessor.Value;
        var response = new GetNotesLimitsToolResponse(
            options.MaxInlineContentBytes,
            options.MaxUploadChunkBytes,
            options.MaxUploadBytes,
            options.MaxUploadBytes,
            options.UploadIdleTimeoutSeconds,
            options.MaxPageContentBytes,
            options.MaxListItemsLimit,
            ITipTapContentService.MaxDepth,
            ITipTapContentService.MaxNodeCount,
            ITipTapContentService.MaxTextLength,
            ["tiptap_json", "tiptap_blocks_json", "markdown"],
            ["text/markdown"]);

        return NotesMcpResultMapper.Success(response, "MCP limits loaded.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.PrepareHttpUpload,
        Title = "Prepare HTTP Upload",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(PrepareHttpUploadToolResponse))]
    [Description("For local Markdown files, call this tool FIRST. It returns the HTTP request details needed to upload a file directly via POST /api/mcp/uploads/markdown using the same OAuth bearer token used for MCP. The client must then perform the returned HTTP request with multipart/form-data and pass the returned uploadId to notes_create_page, notes_update_page_content, or notes_append_blocks_to_page. Only fall back to notes_create_upload if the client cannot make HTTP requests.")]
    public CallToolResult PrepareHttpUpload(
        ClaimsPrincipal user,
        IOptions<McpOptions> mcpOptionsAccessor)
    {
        var options = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(user, options.RequiredWriteScopes);
        if (!actorResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(actorResult.Error!);
        }

        var response = new PrepareHttpUploadToolResponse(
            "/api/mcp/uploads/markdown",
            "POST",
            "Bearer token (same OAuth access token used for MCP)",
            "multipart/form-data",
            ["file", "fileName (optional)"],
            options.MaxUploadBytes,
            ["text/markdown"],
            $"After upload, pass the returned uploadId to {NotesMcpToolNames.CreatePage}, {NotesMcpToolNames.UpdatePageContent}, or {NotesMcpToolNames.AppendBlocksToPage}.");

        return NotesMcpResultMapper.Success(response, "HTTP upload plan prepared.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.Search,
        Title = "Search Notes",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(SearchNotesToolResponse))]
    [Description("Search visible notebooks and notebook items, including page plain-text content.")]
    public async Task<CallToolResult> SearchAsync(
        [Description("The search query.")] string query,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        IOptions<McpOptions> mcpOptionsAccessor,
        [Description("Optional notebook slug to scope the search to one notebook.")] string? notebookSlug = null,
        [Description("Search scope: all, notebooks, or items.")] string? scope = null,
        [Description("Maximum number of results to return.")] int? limit = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredReadScopes);
        if (!actorResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(actorResult.Error!);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_query",
                "The search query is required."));
        }

        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();
        if (normalizedScope is not ("all" or "notebooks" or "items"))
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_scope",
                "Scope must be all, notebooks, or items."));
        }

        var currentUserId = actorResult.Value;
        var maxResults = Math.Clamp(limit ?? 25, 1, mcpOptions.MaxListItemsLimit);
        var results = new List<NotebookSearchResultResponse>();

        if (!string.IsNullOrWhiteSpace(notebookSlug))
        {
            var scopedNotebook = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookReadService, cancellationToken);
            if (!scopedNotebook.Succeeded)
            {
                return NotesMcpResultMapper.Failure(scopedNotebook.Error!);
            }

            var notebook = scopedNotebook.Value!;
            if (normalizedScope is "all" or "notebooks")
            {
                if (NotesMcpSupport.MatchesNotebook(notebook, query))
                {
                    results.Add(new NotebookSearchResultResponse(
                        notebook.Id,
                        notebook.Slug,
                        notebook.Title,
                        NotesMcpSupport.BuildNotebookUri(notebook.Slug),
                        NotesMcpSupport.BuildNotebookItemsUri(notebook.Slug),
                        null,
                        null,
                        null,
                        "notebook",
                        null,
                        notebook.Description,
                        notebook.CanEdit,
                        notebook.UpdatedAtUtc));
                }
            }

            if (normalizedScope is "all" or "items")
            {
                var remainingResults = maxResults - results.Count;
                if (remainingResults > 0)
                {
                    var itemResults = await notebookReadService.GetNotebookItemsAsync(
                        notebook.Id,
                        currentUserId,
                        query,
                        cancellationToken,
                        limit: remainingResults);
                    if (itemResults.Succeeded)
                    {
                        results.AddRange(itemResults.Value!.Select(item => new NotebookSearchResultResponse(
                            notebook.Id,
                            notebook.Slug,
                            notebook.Title,
                            NotesMcpSupport.BuildNotebookUri(notebook.Slug),
                            NotesMcpSupport.BuildNotebookItemsUri(notebook.Slug),
                            item.Id,
                            item.Path,
                            item.Title,
                            item.Type,
                            NotesMcpSupport.BuildItemResourceUri(notebook.Slug, item),
                            NotesMcpSupport.BuildPlainTextSnippet(item.PlainTextContent, query),
                            notebook.CanEdit,
                            item.UpdatedAtUtc)));
                    }
                }
            }
        }
        else
        {
            if (normalizedScope is "all" or "notebooks")
            {
                var publicNotebooks = await notebookReadService.GetPublicNotebooksAsync(query, currentUserId, cancellationToken, maxResults);
                var myNotebooks = await notebookReadService.GetMyNotebooksAsync(currentUserId, query, cancellationToken, maxResults);
                var summaries = publicNotebooks
                    .Concat(myNotebooks)
                    .GroupBy(notebook => notebook.Id)
                    .Select(group => group.First())
                    .OrderByDescending(notebook => notebook.LastActivityAtUtc)
                    .ThenBy(notebook => notebook.Title, StringComparer.OrdinalIgnoreCase)
                    .Take(maxResults);

                results.AddRange(summaries.Select(notebook => new NotebookSearchResultResponse(
                    notebook.Id,
                    notebook.Slug,
                    notebook.Title,
                    NotesMcpSupport.BuildNotebookUri(notebook.Slug),
                    NotesMcpSupport.BuildNotebookItemsUri(notebook.Slug),
                    null,
                    null,
                    null,
                    "notebook",
                    null,
                    notebook.Description,
                    notebook.CanEdit,
                    notebook.UpdatedAtUtc)));
            }

            if (normalizedScope is "all" or "items")
            {
                var remainingResults = maxResults - results.Count;
                if (remainingResults > 0)
                {
                    var itemResults = await notebookReadService.SearchVisibleNotebookItemsAsync(
                        currentUserId,
                        query,
                        cancellationToken,
                        remainingResults);
                    results.AddRange(itemResults.Select(item => new NotebookSearchResultResponse(
                        item.NotebookId,
                        item.NotebookSlug,
                        item.NotebookTitle,
                        NotesMcpSupport.BuildNotebookUri(item.NotebookSlug),
                        NotesMcpSupport.BuildNotebookItemsUri(item.NotebookSlug),
                        item.ItemId,
                        item.Path,
                        item.Title,
                        item.Type,
                        string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase)
                            ? NotesMcpSupport.BuildPageUri(item.NotebookSlug, item.Path!)
                            : null,
                        NotesMcpSupport.BuildPlainTextSnippet(item.PlainTextContent, query),
                        item.NotebookCanEdit,
                        item.UpdatedAtUtc)));
                }
            }
        }

        var trimmedResults = results.Take(maxResults).ToList();
        var response = new SearchNotesToolResponse(query.Trim(), trimmedResults.Count, trimmedResults);
        return NotesMcpResultMapper.Success(response, $"Found {response.TotalCount} result(s) for '{response.Query}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.CreateNotebook,
        Title = "Create Notebook",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(GetNotebookToolResponse))]
    [Description("Create a notebook owned by the authenticated actor.")]
    public async Task<CallToolResult> CreateNotebookAsync(
        [Description("Notebook title.")] string title,
        ClaimsPrincipal user,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional notebook description.")] string? description = null,
        [Description("Notebook visibility: private, unlisted, or public. Defaults to private.")] string? visibility = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.CreateNotebook,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredWriteScopes);
                if (!actorResult.Succeeded)
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(actorResult.Error!);
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_title",
                        "Notebook title is required and cannot be empty or whitespace."));
                }

                if (!string.IsNullOrWhiteSpace(visibility) && visibility is not ("public" or "unlisted" or "private"))
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_visibility",
                        "Visibility must be public, unlisted, or private."));
                }

                var createResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new CreateNotebookCommand(
                        actorResult.Value,
                        title,
                        description,
                        visibility),
                    ct);
                if (!createResult.Succeeded)
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(createResult.Error!, createResult.Value?.Id);
                }

                var response = NotesMcpSupport.ToGetNotebookToolResponse(createResult.Value!);
                return McpMutationResult<GetNotebookToolResponse>.Success(
                    response,
                    $"Notebook '{response.Title}' created.",
                    response.Id,
                    itemId: null);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.UpdateNotebook,
        Title = "Update Notebook",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(GetNotebookToolResponse))]
    [Description("Update notebook title, description, or visibility.")]
    public async Task<CallToolResult> UpdateNotebookAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional new notebook title.")] string? title = null,
        [Description("Optional new notebook description. Use an empty string to clear it.")] string? description = null,
        [Description("Optional new notebook visibility: private, unlisted, or public.")] string? visibility = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.UpdateNotebook,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
                    notebookSlug,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(notebookContextResult.Error!);
                }

                if (string.IsNullOrWhiteSpace(title) && description is null && string.IsNullOrWhiteSpace(visibility))
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "missing_changes",
                        "Specify at least one notebook field to update."));
                }

                if (title is not null && string.IsNullOrWhiteSpace(title))
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_title",
                        "Notebook title cannot be empty or whitespace."));
                }

                if (!string.IsNullOrWhiteSpace(visibility) && visibility is not ("public" or "unlisted" or "private"))
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_visibility",
                        "Visibility must be public, unlisted, or private."));
                }

                var notebookContext = notebookContextResult.Value;
                var notebook = notebookContext.Notebook;
                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookCommand(
                        notebook.Id,
                        notebookContext.ActorId,
                        string.IsNullOrWhiteSpace(title) ? notebook.Title : title,
                        description is null ? notebook.Description : description,
                        string.IsNullOrWhiteSpace(visibility) ? notebook.Visibility : visibility),
                    ct);
                if (!updateResult.Succeeded)
                {
                    return McpMutationResult<GetNotebookToolResponse>.Failure(updateResult.Error!, notebook.Id);
                }

                var response = NotesMcpSupport.ToGetNotebookToolResponse(updateResult.Value!);
                return McpMutationResult<GetNotebookToolResponse>.Success(
                    response,
                    $"Notebook '{response.Title}' updated.",
                    response.Id,
                    itemId: null);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.DeleteNotebook,
        Title = "Delete Notebook",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(DeleteNotebookToolResponse))]
    [Description("Delete a notebook and all of its contents.")]
    public async Task<CallToolResult> DeleteNotebookAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.DeleteNotebook,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
                    notebookSlug,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<DeleteNotebookToolResponse>.Failure(notebookContextResult.Error!);
                }

                var notebookContext = notebookContextResult.Value;
                var notebook = notebookContext.Notebook;
                var deleteResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new DeleteNotebookCommand(
                        notebook.Id,
                        notebookContext.ActorId),
                    ct);
                if (!deleteResult.Succeeded)
                {
                    return McpMutationResult<DeleteNotebookToolResponse>.Failure(deleteResult.Error!, notebook.Id);
                }

                var response = new DeleteNotebookToolResponse(notebook.Id, notebook.Slug, "deleted");
                return McpMutationResult<DeleteNotebookToolResponse>.Success(
                    response,
                    $"Notebook '{notebook.Title}' deleted.",
                    notebook.Id,
                    itemId: null);
            },
            cancellationToken);
    }
}
