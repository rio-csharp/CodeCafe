using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Modules.Notes.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Shared.Application.Configuration;
using MediatR;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

public sealed partial class NotesMcpItemTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.CreateFolder,
        Title = "Create Folder",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(CreateItemToolResponse))]
    [Description("Create a folder in a notebook under an optional parent folder path. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> CreateFolderAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The folder title.")] string title,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional parent folder path. Null creates the folder at the notebook root. " + PathCompatibilityDescription)] string? parentPath = null,
        [Description("Sort order within the parent folder.")] int? sortOrder = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.CreateFolder,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var notebookContextResult = await NotesMcpSupport.RequireNotebookSummaryContextAsync(
                    notebookSlug,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<CreateItemToolResponse>.Failure(notebookContextResult.Error!);
                }

                var notebookContext = notebookContextResult.Value;
                var parentResult = await NotesMcpSupport.ResolveParentAsync(
                    notebookContext.Notebook,
                    parentPath,
                    notebookContext.ActorId,
                    notebookReadService,
                    ct);
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

                var createResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new CreateNotebookItemCommand(
                        notebookContext.Notebook.Id,
                        notebookContext.ActorId,
                        parentResult.Value?.Id,
                        "folder",
                        title,
                        sortOrder ?? 0,
                        null),
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
    [Description("Create a page in a notebook under an optional parent folder path. Accepts small inline TipTap JSON or uploaded Markdown / TipTap JSON for larger body content. " + PageContentLimitDescription + " " + PathCompatibilityDescription)]
    public async Task<CallToolResult> CreatePageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page title.")] string title,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpContentImportService contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional parent folder path. Null creates the page at the notebook root. " + PathCompatibilityDescription)] string? parentPath = null,
        [Description("Sort order within the parent folder.")] int? sortOrder = null,
        [Description("Optional TipTap JSON document for the page body. Use for smaller inline payloads. " + PageContentLimitDescription)] JsonElement? contentJson = null,
        [Description("Optional upload id returned by notes_create_upload for larger Markdown or JSON body content. " + PageContentLimitDescription)] string? contentUploadId = null,
        [Description("Format of contentUploadId: tiptap_json or markdown. Markdown is converted server-side into TipTap JSON. When omitted, the server infers it from the file name or media type.")] string? contentFormat = null,
        [Description("Whether to include full contentJson and plainTextContent in the response. Defaults to false to keep write responses small.")] bool includeContent = false)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.CreatePage,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var notebookContextResult = await NotesMcpSupport.RequireNotebookSummaryContextAsync(
                    notebookSlug,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<CreatePageToolResponse>.Failure(notebookContextResult.Error!);
                }

                var notebookContext = notebookContextResult.Value;
                var parentResult = await NotesMcpSupport.ResolveParentAsync(
                    notebookContext.Notebook,
                    parentPath,
                    notebookContext.ActorId,
                    notebookReadService,
                    ct);
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

                var contentJsonResult = await contentImportService.ResolveOptionalPageContentAsync(
                    notebookContext.ActorId,
                    contentJson,
                    contentUploadId,
                    contentFormat,
                    "invalid_content_json",
                    "contentJson must be a TipTap document object, or contentUploadId must reference uploaded Markdown or TipTap JSON.",
                    ct);
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

                var createResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new CreateNotebookItemCommand(
                        notebookContext.Notebook.Id,
                        notebookContext.ActorId,
                        parentResult.Value?.Id,
                        "page",
                        title,
                        sortOrder ?? 0,
                        contentJsonResult.Value),
                    ct);
                if (!createResult.Succeeded)
                {
                    return McpMutationResult<CreatePageToolResponse>.Failure(
                        createResult.Error!,
                        notebookContext.Notebook.Id,
                        createResult.Value?.Id);
                }

                var response = NotesMcpSupport.ToCreatePageToolResponse(notebookContext.Notebook, createResult.Value!, includeContent);
                await contentImportService.DeleteUploadAsync(notebookContext.ActorId, contentUploadId, ct);
                return McpMutationResult<CreatePageToolResponse>.Success(
                    response,
                    $"Page '{response.Title}' created.",
                    notebookContext.Notebook.Id,
                    response.PageId);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.UpdatePageContent,
        Title = "Update Page Content",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Replace a page's stored body content. Accepts small inline TipTap JSON or uploaded Markdown / TipTap JSON for larger edits. Markdown is converted server-side into TipTap JSON. " + PageContentLimitDescription + " " + PathCompatibilityDescription)]
    public async Task<CallToolResult> UpdatePageContentAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path. " + PathCompatibilityDescription)] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpContentImportService contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("The full TipTap JSON document to store as page body. Use for smaller inline payloads. " + PageContentLimitDescription)] JsonElement? contentJson = null,
        [Description("Optional upload id returned by notes_create_upload for larger Markdown or JSON body content. " + PageContentLimitDescription)] string? contentUploadId = null,
        [Description("Format of contentUploadId: tiptap_json or markdown. Markdown is converted server-side into TipTap JSON. When omitted, the server infers it from the file name or media type.")] string? contentFormat = null,
        [Description("Whether to include full contentJson and plainTextContent in the response. Defaults to false to keep write responses small.")] bool includeContent = false)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.UpdatePageContent,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var pageContextResult = await NotesMcpSupport.RequirePageSummaryContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!pageContextResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(pageContextResult.Error!);
                }

                var contentJsonResult = await contentImportService.ResolveRequiredPageContentAsync(
                    pageContextResult.Value.ActorId,
                    contentJson,
                    contentUploadId,
                    contentFormat,
                    "invalid_content_json",
                    "contentJson must be a TipTap document object, or contentUploadId must reference uploaded Markdown or TipTap JSON.",
                    ct);
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
                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        pageContext.Notebook.Id,
                        pageContext.Item.Id,
                        pageContext.ActorId,
                        pageContext.Item.Title,
                        default,
                        null,
                        contentJsonResult.Value,
                        expectedUpdatedAtUtc),
                    ct);
                if (!updateResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        updateResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var response = NotesMcpSupport.ToUpdatePageContentToolResponse(pageContext.Notebook, updateResult.Value!, includeContent);
                await contentImportService.DeleteUploadAsync(pageContext.ActorId, contentUploadId, ct);
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Page '{response.Title}' content updated.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }
}
