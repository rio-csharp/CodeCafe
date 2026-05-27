using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Interfaces;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace CodeCafe.WebApi.Mcp;

[McpServerToolType]
public sealed class NotesMcpNotebookTools
{
    [McpServerTool(
        Name = "notes.list_notebooks",
        Title = "List Notebooks",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ListNotebooksToolResponse))]
    [Description("List notebooks visible to the authenticated actor.")]
    public async Task<CallToolResult> ListNotebooksAsync(
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
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

        var maxResults = Math.Clamp(limit ?? 25, 1, 100);
        var notebooks = new List<NotebookSummaryModel>();

        if (normalizedScope is "all" or "mine")
        {
            notebooks.AddRange(await notebookQueryService.GetMyNotebooksAsync(currentUserId, query, cancellationToken, maxResults));
        }

        if (normalizedScope is "all" or "public")
        {
            notebooks.AddRange(await notebookQueryService.GetPublicNotebooksAsync(query, currentUserId, cancellationToken, maxResults));
        }

        var notebookDetails = notebooks
            .GroupBy(notebook => notebook.Id)
            .Select(group => group.First())
            .Take(maxResults)
            .Select(NotesMcpSupport.ToGetNotebookToolResponse)
            .ToList();

        var response = new ListNotebooksToolResponse(normalizedScope, notebookDetails.Count, notebookDetails);
        return NotesMcpResultMapper.Success(response, $"Listed {response.TotalCount} notebook(s) for scope '{response.Scope}'.");
    }

    [McpServerTool(
        Name = "notes.get_notebook",
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
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(slug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var response = NotesMcpSupport.ToGetNotebookToolResponse(notebookResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Notebook '{response.Title}' loaded.");
    }

    [McpServerTool(
        Name = "notes.search",
        Title = "Search Notes",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(SearchNotesToolResponse))]
    [Description("Search visible notebooks and notebook items.")]
    public async Task<CallToolResult> SearchAsync(
        [Description("The search query.")] string query,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
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
        var maxResults = Math.Clamp(limit ?? 25, 1, 100);
        var results = new List<NotebookSearchResultResponse>();
        var notebooks = new List<NotebookDetailModel>();

        if (!string.IsNullOrWhiteSpace(notebookSlug))
        {
            var scopedNotebook = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
            if (!scopedNotebook.Succeeded)
            {
                return NotesMcpResultMapper.Failure(scopedNotebook.Error!);
            }

            notebooks.Add(scopedNotebook.Value!);
        }
        else
        {
            var publicNotebooks = await notebookQueryService.GetPublicNotebooksAsync(query, currentUserId, cancellationToken, maxResults);
            var myNotebooks = await notebookQueryService.GetMyNotebooksAsync(currentUserId, query, cancellationToken, maxResults);
            var summaries = publicNotebooks
                .Concat(myNotebooks)
                .GroupBy(notebook => notebook.Id)
                .Select(group => group.First())
                .Take(maxResults)
                .ToList();

            foreach (var summary in summaries)
            {
                var notebookResult = await notebookQueryService.GetNotebookBySlugAsync(summary.Slug, currentUserId, cancellationToken);
                if (notebookResult.Succeeded)
                {
                    notebooks.Add(notebookResult.Value!);
                }
            }
        }

        foreach (var notebook in notebooks)
        {
            if (normalizedScope is "all" or "notebooks")
            {
                if (NotesMcpSupport.MatchesNotebook(notebook, query))
                {
                    results.Add(new NotebookSearchResultResponse(
                        notebook.Id,
                        notebook.Slug,
                        notebook.Title,
                        null,
                        null,
                        null,
                        "notebook",
                        notebook.Description,
                        notebook.CanEdit,
                        notebook.UpdatedAtUtc));
                }
            }

            if (normalizedScope is "all" or "items")
            {
                var remainingResults = maxResults - results.Count;
                if (remainingResults <= 0)
                {
                    break;
                }

                var itemResults = await notebookQueryService.GetNotebookItemsAsync(
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
                        item.Id,
                        item.Path,
                        item.Title,
                        item.Type,
                        NotesMcpSupport.BuildPlainTextSnippet(item.PlainTextContent, query),
                        notebook.CanEdit,
                        item.UpdatedAtUtc)));
                }
            }

            if (results.Count >= maxResults)
            {
                break;
            }
        }

        var trimmedResults = results.Take(maxResults).ToList();
        var response = new SearchNotesToolResponse(query.Trim(), trimmedResults.Count, trimmedResults);
        return NotesMcpResultMapper.Success(response, $"Found {response.TotalCount} result(s) for '{response.Query}'.");
    }

    [McpServerTool(
        Name = "notes.create_notebook",
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
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional notebook description.")] string? description = null,
        [Description("Notebook visibility: private, unlisted, or public. Defaults to private.")] string? visibility = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredWriteScopes);
        if (!actorResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(actorResult.Error!);
        }

        var createResult = await notebookCommandService.CreateNotebookAsync(
            actorResult.Value,
            title,
            description,
            visibility,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.create_notebook", createResult.Value?.Id, null, createResult, cancellationToken);

        if (!createResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(createResult.Error!);
        }

        var response = NotesMcpSupport.ToGetNotebookToolResponse(createResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Notebook '{response.Title}' created.");
    }

    [McpServerTool(
        Name = "notes.update_notebook",
        Title = "Update Notebook",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(GetNotebookToolResponse))]
    [Description("Update notebook title, description, or visibility.")]
    public async Task<CallToolResult> UpdateNotebookAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional new notebook title.")] string? title = null,
        [Description("Optional new notebook description. Use an empty string to clear it.")] string? description = null,
        [Description("Optional new notebook visibility: private, unlisted, or public.")] string? visibility = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        if (string.IsNullOrWhiteSpace(title) && description is null && string.IsNullOrWhiteSpace(visibility))
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "missing_changes",
                "Specify at least one notebook field to update."));
        }

        var notebook = notebookResult.Value!;
        var updateResult = await notebookCommandService.UpdateNotebookAsync(
            notebook.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            string.IsNullOrWhiteSpace(title) ? notebook.Title : title,
            description is null ? notebook.Description : description,
            string.IsNullOrWhiteSpace(visibility) ? notebook.Visibility : visibility,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.update_notebook", notebook.Id, null, updateResult, cancellationToken);

        if (!updateResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(updateResult.Error!);
        }

        var response = NotesMcpSupport.ToGetNotebookToolResponse(updateResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Notebook '{response.Title}' updated.");
    }

    [McpServerTool(
        Name = "notes.delete_notebook",
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
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var notebook = notebookResult.Value!;
        var deleteResult = await notebookCommandService.DeleteNotebookAsync(
            notebook.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.delete_notebook", notebook.Id, null, deleteResult, cancellationToken);

        if (!deleteResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(deleteResult.Error!);
        }

        var response = new DeleteNotebookToolResponse(notebook.Id, notebook.Slug, "deleted");
        return NotesMcpResultMapper.Success(response, $"Notebook '{notebook.Title}' deleted.");
    }
}
