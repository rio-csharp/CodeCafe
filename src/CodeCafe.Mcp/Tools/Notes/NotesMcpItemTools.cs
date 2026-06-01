using CodeCafe.Mcp.Configuration;
using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Application.Notes;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;

namespace CodeCafe.Mcp.Tools.Notes;

[McpServerToolType]
public sealed class NotesMcpItemTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.ListItems,
        Title = "List Notebook Items",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ListNotebookItemsToolResponse))]
    [Description("List folder and page items visible to the authenticated actor, with optional parent, type, archive, and pagination filters.")]
    public async Task<CallToolResult> ListItemsAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        CancellationToken cancellationToken,
        IOptions<McpOptions> mcpOptionsAccessor,
        [Description("Optional search term to filter notebook items.")] string? search = null,
        [Description("Optional parent folder path. When provided, only direct children of that folder are returned.")] string? parentPath = null,
        [Description("Filter item type: all, page, or folder.")] string? type = null,
        [Description("Include archived items in the result set. Only the notebook owner can use this.")] bool includeArchived = false,
        [Description("Zero-based offset for pagination.")] int? offset = null,
        [Description("Maximum number of items to return.")] int? limit = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
            notebookSlug,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredReadScopes,
            includeArchived);
        if (!notebookContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookContextResult.Error!);
        }

        var notebookContext = notebookContextResult.Value;
        var notebook = notebookContext.Notebook;
        if (includeArchived && notebook.OwnerId != notebookContext.ActorId)
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "Only the notebook owner can view archived items."));
        }

        var itemsResult = await notebookQueryService.GetNotebookItemsAsync(
            notebook.Id,
            notebookContext.ActorId,
            search,
            cancellationToken,
            includeArchived);

        if (!itemsResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemsResult.Error!);
        }

        var normalizedType = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim().ToLowerInvariant();
        if (normalizedType is not ("all" or "page" or "folder"))
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_type",
                "Type must be all, page, or folder."));
        }

        Guid? parentIdFilter = null;
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            var parentResult = NotesMcpSupport.RequireItem(notebook, parentPath);
            if (!parentResult.Succeeded)
            {
                return NotesMcpResultMapper.Failure(parentResult.Error!);
            }

            if (!string.Equals(parentResult.Value!.Type, "folder", StringComparison.OrdinalIgnoreCase))
            {
                return NotesMcpResultMapper.Failure(new NotesError(
                    NotesFailureKind.Validation,
                    "invalid_parent",
                    "Parent item must be a folder."));
            }

            parentIdFilter = parentResult.Value.Id;
        }

        var filteredItems = itemsResult.Value!
            .Where(item => normalizedType == "all" || string.Equals(item.Type, normalizedType, StringComparison.OrdinalIgnoreCase))
            .Where(item => parentIdFilter is null || item.ParentId == parentIdFilter)
            .ToList();

        var normalizedOffset = Math.Max(0, offset ?? 0);
        var maxLimit = Math.Clamp(limit ?? 100, 1, mcpOptions.MaxListItemsLimit);
        var pagedItems = filteredItems
            .Skip(normalizedOffset)
            .Take(maxLimit)
            .Select(item => NotesMcpSupport.ToNotebookItemToolResponse(notebook, item))
            .ToList();

        var response = new ListNotebookItemsToolResponse(
            notebook.Id,
            notebook.Slug,
            notebook.Title,
            notebook.CanEdit,
            filteredItems.Count,
            normalizedOffset,
            pagedItems.Count,
            pagedItems);

        return NotesMcpResultMapper.Success(response, $"Listed {response.Items.Count} item(s) for notebook '{response.NotebookTitle}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.GetPage,
        Title = "Get Page",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(GetPageToolResponse))]
    [Description("Read one page by notebook slug and page path, including the stored TipTap JSON string and derived plain text.")]
    public async Task<CallToolResult> GetPageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path within the notebook.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var pageContextResult = await NotesMcpSupport.RequirePageContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredReadScopes);
        if (!pageContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(pageContextResult.Error!);
        }

        var pageContext = pageContextResult.Value;
        var response = NotesMcpSupport.ToGetPageToolResponse(pageContext.Notebook, pageContext.Item);
        return NotesMcpResultMapper.Success(response, $"Page '{response.Title}' loaded.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.CreateUpload,
        Title = "Create Upload",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(CreateUploadToolResponse))]
    [Description("Create an in-memory MCP upload session for chunked local content such as Markdown or TipTap JSON. Preferred for remote clients and larger payloads.")]
    public async Task<CallToolResult> CreateUpload(
        ClaimsPrincipal user,
        IOptions<McpOptions> mcpOptionsAccessor,
        IMcpUploadStore uploadStore,
        IMcpAuditService auditService,
        ILogger<NotesMcpItemTools> logger,
        CancellationToken cancellationToken,
        [Description("Optional original file name, such as notes.md or page.json. Used for format inference.")] string? fileName = null,
        [Description("The media type for the upload, such as text/markdown or application/json. Used for format inference when contentFormat is omitted later.")] string? mediaType = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredWriteScopes);
        if (!actorResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(actorResult.Error!);
        }

        var session = uploadStore.Create(actorResult.Value, fileName, string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim());
        var response = new CreateUploadToolResponse(
            session.UploadId,
            session.FileName,
            session.MediaType,
            session.BytesReceived,
            session.CreatedAtUtc);

        await WriteUploadObservationAsync(
            auditService,
            logger,
            actorResult.Value,
            NotesMcpToolNames.CreateUpload,
            session.UploadId,
            succeeded: true,
            resultCode: "success",
            errorCode: null,
            bytesReceived: session.BytesReceived,
            cancellationToken);

        return NotesMcpResultMapper.Success(response, $"Upload '{response.UploadId}' created.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.AppendUploadChunk,
        Title = "Append Upload Chunk",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(AppendUploadChunkToolResponse))]
    [Description("Append UTF-8 text to an in-memory upload session. Use this for local Markdown or JSON files instead of assuming shared server storage.")]
    public async Task<CallToolResult> AppendUploadChunk(
        [Description("The upload session id returned by notes_create_upload.")] string uploadId,
        [Description("UTF-8 text chunk to append to the upload.")] string chunkText,
        ClaimsPrincipal user,
        IOptions<McpOptions> mcpOptionsAccessor,
        IMcpUploadStore uploadStore,
        IMcpAuditService auditService,
        ILogger<NotesMcpItemTools> logger,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredWriteScopes);
        if (!actorResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(actorResult.Error!);
        }

        var appendResult = uploadStore.AppendText(
            actorResult.Value,
            uploadId,
            chunkText,
            mcpOptions.MaxUploadChunkBytes,
            mcpOptions.MaxUploadBytes);
        if (!appendResult.Succeeded)
        {
            await WriteUploadObservationAsync(
                auditService,
                logger,
                actorResult.Value,
                NotesMcpToolNames.AppendUploadChunk,
                uploadId,
                succeeded: false,
                resultCode: appendResult.Error!.Code,
                errorCode: appendResult.Error.Code,
                bytesReceived: null,
                cancellationToken);

            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                appendResult.Error!.Code,
                appendResult.Error.Message));
        }

        var session = appendResult.Value!;
        var response = new AppendUploadChunkToolResponse(
            session.UploadId,
            session.BytesReceived,
            System.Text.Encoding.UTF8.GetByteCount(chunkText),
            session.BytesReceived > 0);

        await WriteUploadObservationAsync(
            auditService,
            logger,
            actorResult.Value,
            NotesMcpToolNames.AppendUploadChunk,
            session.UploadId,
            succeeded: true,
            resultCode: "success",
            errorCode: null,
            bytesReceived: session.BytesReceived,
            cancellationToken);

        return NotesMcpResultMapper.Success(response, $"Upload '{response.UploadId}' appended.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.DiscardUpload,
        Title = "Discard Upload",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(DiscardUploadToolResponse))]
    [Description("Discard an MCP upload session when it is no longer needed.")]
    public async Task<CallToolResult> DiscardUpload(
        [Description("The upload session id returned by notes_create_upload.")] string uploadId,
        ClaimsPrincipal user,
        IOptions<McpOptions> mcpOptionsAccessor,
        IMcpUploadStore uploadStore,
        IMcpAuditService auditService,
        ILogger<NotesMcpItemTools> logger,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(user, mcpOptions.RequiredWriteScopes);
        if (!actorResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(actorResult.Error!);
        }

        var removed = uploadStore.Delete(actorResult.Value, uploadId);
        if (!removed)
        {
            await WriteUploadObservationAsync(
                auditService,
                logger,
                actorResult.Value,
                NotesMcpToolNames.DiscardUpload,
                uploadId,
                succeeded: false,
                resultCode: "upload_not_found",
                errorCode: "upload_not_found",
                bytesReceived: null,
                cancellationToken);

            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.NotFound,
                "upload_not_found",
                "Upload session was not found."));
        }

        var response = new DiscardUploadToolResponse(uploadId, "discarded");
        await WriteUploadObservationAsync(
            auditService,
            logger,
            actorResult.Value,
            NotesMcpToolNames.DiscardUpload,
            uploadId,
            succeeded: true,
            resultCode: "success",
            errorCode: null,
            bytesReceived: null,
            cancellationToken);

        return NotesMcpResultMapper.Success(response, $"Upload '{uploadId}' discarded.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.CreateFolder,
        Title = "Create Folder",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(CreateItemToolResponse))]
    [Description("Create a folder in a notebook under an optional parent folder path.")]
    public async Task<CallToolResult> CreateFolderAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The folder title.")] string title,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional parent folder path. Null creates the folder at the notebook root.")] string? parentPath = null,
        [Description("Sort order within the parent folder.")] int? sortOrder = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.CreateFolder,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
                    notebookSlug,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<CreateItemToolResponse>.Failure(notebookContextResult.Error!);
                }

                var notebookContext = notebookContextResult.Value;
                var parentResult = NotesMcpSupport.ResolveParent(notebookContext.Notebook, parentPath);
                if (!parentResult.Succeeded)
                {
                    return McpMutationResult<CreateItemToolResponse>.Failure(parentResult.Error!, notebookContext.Notebook.Id);
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return McpMutationResult<CreateItemToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_title",
                        "Folder title is required and cannot be empty or whitespace."),
                        notebookContext.Notebook.Id);
                }

                var createResult = await notebookCommandService.CreateNotebookItemAsync(
                    notebookContext.Notebook.Id,
                    notebookContext.ActorId,
                    parentResult.Value?.Id,
                    "folder",
                    title,
                    sortOrder ?? 0,
                    null,
                    ct);
                if (!createResult.Succeeded)
                {
                    return McpMutationResult<CreateItemToolResponse>.Failure(
                        createResult.Error!,
                        notebookContext.Notebook.Id,
                        createResult.Value?.Id);
                }

                var response = NotesMcpSupport.ToCreateItemToolResponse(notebookContext.Notebook, createResult.Value!);
                return McpMutationResult<CreateItemToolResponse>.Success(
                    response,
                    $"Folder '{response.Title}' created.",
                    notebookContext.Notebook.Id,
                    response.ItemId);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.CreatePage,
        Title = "Create Page",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(CreatePageToolResponse))]
    [Description("Create a page in a notebook under an optional parent folder path. Accepts small inline TipTap JSON or uploaded Markdown / TipTap JSON for larger content.")]
    public async Task<CallToolResult> CreatePageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page title.")] string title,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpContentImportService contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional parent folder path. Null creates the page at the notebook root.")] string? parentPath = null,
        [Description("Sort order within the parent folder.")] int? sortOrder = null,
        [Description("Optional TipTap JSON document for the page content. Use for smaller inline payloads.")] JsonElement? contentJson = null,
        [Description("Optional upload id returned by notes_create_upload for larger Markdown or JSON content.")] string? contentUploadId = null,
        [Description("Format of contentUploadId: tiptap_json or markdown. Markdown is converted server-side into TipTap JSON. When omitted, the server infers it from the file name or media type.")] string? contentFormat = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.CreatePage,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
                    notebookSlug,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<CreatePageToolResponse>.Failure(notebookContextResult.Error!);
                }

                var notebookContext = notebookContextResult.Value;
                var parentResult = NotesMcpSupport.ResolveParent(notebookContext.Notebook, parentPath);
                if (!parentResult.Succeeded)
                {
                    return McpMutationResult<CreatePageToolResponse>.Failure(parentResult.Error!, notebookContext.Notebook.Id);
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return McpMutationResult<CreatePageToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_title",
                        "Page title is required and cannot be empty or whitespace."),
                        notebookContext.Notebook.Id);
                }

                var contentJsonResult = contentImportService.ResolveOptionalPageContent(
                    notebookContext.ActorId,
                    contentJson,
                    contentUploadId,
                    contentFormat,
                    "invalid_content_json",
                    "ContentJson must be valid JSON.");
                if (!contentJsonResult.Succeeded)
                {
                    return McpMutationResult<CreatePageToolResponse>.Failure(contentJsonResult.Error!, notebookContext.Notebook.Id);
                }

                if (contentJsonResult.Value is JsonElement contentValue)
                {
                    var sizeResult = contentImportService.EnforcePageContentSize(contentValue, "content_too_large");
                    if (!sizeResult.Succeeded)
                    {
                        return McpMutationResult<CreatePageToolResponse>.Failure(sizeResult.Error!, notebookContext.Notebook.Id);
                    }
                }

                var createResult = await notebookCommandService.CreateNotebookItemAsync(
                    notebookContext.Notebook.Id,
                    notebookContext.ActorId,
                    parentResult.Value?.Id,
                    "page",
                    title,
                    sortOrder ?? 0,
                    contentJsonResult.Value,
                    ct);
                if (!createResult.Succeeded)
                {
                    return McpMutationResult<CreatePageToolResponse>.Failure(
                        createResult.Error!,
                        notebookContext.Notebook.Id,
                        createResult.Value?.Id);
                }

                var response = NotesMcpSupport.ToCreatePageToolResponse(notebookContext.Notebook, createResult.Value!);
                contentImportService.DeleteUpload(notebookContext.ActorId, contentUploadId);
                return McpMutationResult<CreatePageToolResponse>.Success(
                    response,
                    $"Page '{response.Title}' created.",
                    notebookContext.Notebook.Id,
                    response.PageId);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.UpdatePageContentJson,
        Title = "Update Page Content",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Replace a page's stored content. Accepts small inline TipTap JSON or uploaded Markdown / TipTap JSON for larger edits.")]
    public async Task<CallToolResult> UpdatePageContentJsonAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpContentImportService contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("The full TipTap JSON document to store. Use for smaller inline payloads.")] JsonElement? contentJson = null,
        [Description("Optional upload id returned by notes_create_upload for larger Markdown or JSON content.")] string? contentUploadId = null,
        [Description("Format of contentUploadId: tiptap_json or markdown. Markdown is converted server-side into TipTap JSON. When omitted, the server infers it from the file name or media type.")] string? contentFormat = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.UpdatePageContentJson,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var pageContextResult = await NotesMcpSupport.RequirePageContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!pageContextResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(pageContextResult.Error!);
                }

                var contentJsonResult = contentImportService.ResolveRequiredPageContent(
                    pageContextResult.Value.ActorId,
                    contentJson,
                    contentUploadId,
                    contentFormat,
                    "invalid_content_json",
                    "ContentJson must be valid JSON.");
                if (!contentJsonResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        contentJsonResult.Error!,
                        pageContextResult.Value.Notebook.Id,
                        pageContextResult.Value.Item.Id);
                }

                var sizeResult = contentImportService.EnforcePageContentSize(contentJsonResult.Value, "content_too_large");
                if (!sizeResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        sizeResult.Error!,
                        pageContextResult.Value.Notebook.Id,
                        pageContextResult.Value.Item.Id);
                }

                var pageContext = pageContextResult.Value;
                var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
                    pageContext.Notebook.Id,
                    pageContext.Item.Id,
                    pageContext.ActorId,
                    pageContext.Item.Title,
                    default,
                    null,
                    contentJsonResult.Value,
                    ct,
                    expectedUpdatedAtUtc);
                if (!updateResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        updateResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var response = NotesMcpSupport.ToUpdatePageContentToolResponse(pageContext.Notebook, updateResult.Value!);
                contentImportService.DeleteUpload(pageContext.ActorId, contentUploadId);
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Page '{response.Title}' content updated.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.AppendBlocksToPage,
        Title = "Append Blocks To Page",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Append block content to an existing page document. Accepts small inline TipTap blocks JSON or uploaded Markdown / TipTap blocks JSON for larger additions.")]
    public async Task<CallToolResult> AppendBlocksToPageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpContentImportService contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("The TipTap block nodes JSON array to append. Use for smaller inline payloads.")] JsonElement? blocks = null,
        [Description("Optional upload id returned by notes_create_upload for larger TipTap blocks JSON or Markdown content.")] string? blocksUploadId = null,
        [Description("Format of blocksUploadId: tiptap_blocks_json or markdown. Markdown is converted server-side into TipTap blocks before append. When omitted, the server infers it from the file name or media type.")] string? blocksFormat = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.AppendBlocksToPage,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var pageContextResult = await NotesMcpSupport.RequirePageContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!pageContextResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(pageContextResult.Error!);
                }

                var blocksResult = contentImportService.ResolveRequiredBlocks(
                    pageContextResult.Value.ActorId,
                    blocks,
                    blocksUploadId,
                    blocksFormat,
                    "invalid_blocks",
                    "Blocks must be valid JSON.");
                if (!blocksResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        blocksResult.Error!,
                        pageContextResult.Value.Notebook.Id,
                        pageContextResult.Value.Item.Id);
                }

                if (blocksResult.Value.ValueKind != JsonValueKind.Array)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_blocks",
                        "Blocks must be a JSON array."),
                        pageContextResult.Value.Notebook.Id,
                        pageContextResult.Value.Item.Id);
                }

                var pageContext = pageContextResult.Value;
                var nextContentJson = NotesMcpSupport.AppendBlocks(pageContext.Item.ContentJson, blocksResult.Value);
                var sizeResult = contentImportService.EnforcePageContentSize(nextContentJson, "content_too_large");
                if (!sizeResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        sizeResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
                    pageContext.Notebook.Id,
                    pageContext.Item.Id,
                    pageContext.ActorId,
                    pageContext.Item.Title,
                    default,
                    null,
                    nextContentJson,
                    ct,
                    expectedUpdatedAtUtc);
                if (!updateResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        updateResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var response = NotesMcpSupport.ToUpdatePageContentToolResponse(pageContext.Notebook, updateResult.Value!);
                contentImportService.DeleteUpload(pageContext.ActorId, blocksUploadId);
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Appended blocks to page '{response.Title}'.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.RenameItem,
        Title = "Rename Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Rename a page or folder while keeping its current parent and content.")]
    public async Task<CallToolResult> RenameItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The current item path.")] string path,
        [Description("The new title for the page or folder.")] string title,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.RenameItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_title",
                        "Title cannot be empty or whitespace."),
                        itemContextResult.Value.Notebook.Id,
                        itemContextResult.Value.Item.Id);
                }

                var itemContext = itemContextResult.Value;
                var renameResult = await notebookCommandService.UpdateNotebookItemAsync(
                    itemContext.Notebook.Id,
                    itemContext.Item.Id,
                    itemContext.ActorId,
                    title,
                    default,
                    null,
                    default,
                    ct,
                    expectedUpdatedAtUtc);
                if (!renameResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        renameResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var refreshedNotebook = await notebookQueryService.GetNotebookBySlugAsync(notebookSlug, itemContext.ActorId, ct);
                if (!refreshedNotebook.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        refreshedNotebook.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(refreshedNotebook.Value!, renameResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Item renamed to '{response.Title}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.MoveItem,
        Title = "Move Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Move a page or folder to a new parent folder path or to the notebook root.")]
    public async Task<CallToolResult> MoveItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The current item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("The target parent folder path. Null moves the item to the notebook root.")] string? targetParentPath = null,
        [Description("Optional new sort order.")] int? sortOrder = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.MoveItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var parentResult = NotesMcpSupport.ResolveParent(itemContext.Notebook, targetParentPath);
                if (!parentResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        parentResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
                    itemContext.Notebook.Id,
                    itemContext.Item.Id,
                    itemContext.ActorId,
                    itemContext.Item.Title,
                    NotesMcpSupport.ToGuidJsonElement(parentResult.Value?.Id),
                    sortOrder,
                    default,
                    ct);
                if (!updateResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        updateResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, updateResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Item '{response.Title}' moved to '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ReorderItems,
        Title = "Reorder Items",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ReorderItemsToolResponse))]
    [Description("Batch reorder and optionally move items within a notebook.")]
    public async Task<CallToolResult> ReorderItemsAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The set of item reorder operations.")] IReadOnlyList<ReorderNotesItemRequest> items,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.ReorderItems,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                if (items.Count == 0)
                {
                    return McpMutationResult<ReorderItemsToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_items",
                        "At least one reorder item is required."));
                }

                var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
                    notebookSlug,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<ReorderItemsToolResponse>.Failure(notebookContextResult.Error!);
                }

                var notebookContext = notebookContextResult.Value;
                var itemsByPath = notebookContext.Notebook.Items.ToDictionary(
                    i => i.Path,
                    i => i,
                    StringComparer.Ordinal);
                var reorderModels = new List<ReorderNotebookItemModel>();
                foreach (var item in items)
                {
                    var normalizedPath = NotesMcpSupport.NormalizePath(item.Path);
                    if (!itemsByPath.TryGetValue(normalizedPath, out var resolvedItem))
                    {
                        return McpMutationResult<ReorderItemsToolResponse>.Failure(new NotesError(
                            NotesFailureKind.NotFound,
                            "notebook_item_not_found",
                            "Notebook item was not found."),
                            notebookContext.Notebook.Id);
                    }

                    var resolvedParent = NotesMcpSupport.ResolveParent(notebookContext.Notebook, item.ParentPath);
                    if (!resolvedParent.Succeeded)
                    {
                        return McpMutationResult<ReorderItemsToolResponse>.Failure(
                            resolvedParent.Error!,
                            notebookContext.Notebook.Id);
                    }

                    reorderModels.Add(new ReorderNotebookItemModel(
                        resolvedItem.Id,
                        resolvedParent.Value?.Id,
                        item.SortOrder));
                }

                var reorderResult = await notebookCommandService.ReorderNotebookItemsAsync(
                    notebookContext.Notebook.Id,
                    notebookContext.ActorId,
                    reorderModels,
                    ct);
                if (!reorderResult.Succeeded)
                {
                    return McpMutationResult<ReorderItemsToolResponse>.Failure(
                        reorderResult.Error!,
                        notebookContext.Notebook.Id);
                }

                var response = new ReorderItemsToolResponse(
                    notebookContext.Notebook.Id,
                    notebookContext.Notebook.Slug,
                    reorderResult.Value!.Select(item => NotesMcpSupport.ToNotebookItemToolResponse(notebookContext.Notebook, item)).ToList());

                return McpMutationResult<ReorderItemsToolResponse>.Success(
                    response,
                    $"Reordered {items.Count} item(s) in notebook '{response.NotebookSlug}'.",
                    notebookContext.Notebook.Id,
                    itemId: null);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.DeleteItem,
        Title = "Delete Item",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(DeleteItemToolResponse))]
    [Description("Delete a page or folder and its descendants from a notebook.")]
    public async Task<CallToolResult> DeleteItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.DeleteItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<DeleteItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var deleteResult = await notebookCommandService.DeleteNotebookItemAsync(
                    itemContext.Notebook.Id,
                    itemContext.Item.Id,
                    itemContext.ActorId,
                    ct);
                if (!deleteResult.Succeeded)
                {
                    return McpMutationResult<DeleteItemToolResponse>.Failure(
                        deleteResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = new DeleteItemToolResponse(
                    itemContext.Notebook.Id,
                    itemContext.Notebook.Slug,
                    itemContext.Item.Id,
                    itemContext.Item.Path,
                    "deleted");

                return McpMutationResult<DeleteItemToolResponse>.Success(
                    response,
                    $"Deleted item '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ArchiveItem,
        Title = "Archive Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Archive a page or folder and its descendants.")]
    public async Task<CallToolResult> ArchiveItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.ArchiveItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var archiveResult = await notebookCommandService.ArchiveNotebookItemAsync(
                    itemContext.Notebook.Id,
                    itemContext.Item.Id,
                    itemContext.ActorId,
                    ct);
                if (!archiveResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        archiveResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, archiveResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Archived item '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.RestoreItem,
        Title = "Restore Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Restore an archived page or folder and its descendants.")]
    public async Task<CallToolResult> RestoreItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The archived item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.RestoreItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookQueryService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var restoreResult = await notebookCommandService.RestoreNotebookItemAsync(
                    itemContext.Notebook.Id,
                    itemContext.Item.Id,
                    itemContext.ActorId,
                    ct);
                if (!restoreResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        restoreResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var refreshedNotebook = await notebookQueryService.GetNotebookBySlugAsync(notebookSlug, itemContext.ActorId, ct);
                if (!refreshedNotebook.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        refreshedNotebook.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(refreshedNotebook.Value!, restoreResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Restored item '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    private static McpAuditRecord CreateUploadAuditRecord(
        Guid actorUserId,
        string toolName,
        bool succeeded,
        string resultCode,
        string? errorCode)
        => new(
            actorUserId,
            "user",
            toolName,
            null,
            null,
            succeeded,
            resultCode,
            errorCode);

    private static async Task WriteUploadObservationAsync(
        IMcpAuditService auditService,
        ILogger<NotesMcpItemTools> logger,
        Guid actorUserId,
        string toolName,
        string? uploadId,
        bool succeeded,
        string resultCode,
        string? errorCode,
        int? bytesReceived,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "MCP upload tool completed. ActorUserId={ActorUserId}; ToolName={ToolName}; UploadId={UploadId}; BytesReceived={BytesReceived}; Succeeded={Succeeded}; ResultCode={ResultCode}; ErrorCode={ErrorCode}",
            actorUserId,
            toolName,
            uploadId,
            bytesReceived,
            succeeded,
            resultCode,
            errorCode);

        try
        {
            await auditService.WriteIndependentAsync(
                CreateUploadAuditRecord(actorUserId, toolName, succeeded, resultCode, errorCode),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to write MCP upload audit entry. ActorUserId={ActorUserId}; ToolName={ToolName}; UploadId={UploadId}; ResultCode={ResultCode}",
                actorUserId,
                toolName,
                uploadId,
                resultCode);
        }
    }
}
