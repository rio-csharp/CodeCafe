using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Mcp;
using CodeCafe.Application.Common.Uploads;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Application.Common.Configuration;
using MediatR;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;

namespace CodeCafe.Host.Mcp;

public sealed partial class NotesMcpItemTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.AppendBlocksToPage,
        Title = "Append Blocks To Page",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Append block content to an existing page body. Accepts small inline TipTap blocks JSON, or uploaded Markdown / TipTap blocks JSON for larger additions. Markdown is converted server-side into TipTap blocks before append. " + PageContentLimitDescription + " " + PathCompatibilityDescription)]
    public async Task<CallToolResult> AppendBlocksToPageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path. " + PathCompatibilityDescription)] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("The TipTap block nodes JSON array to append. Use for smaller inline payloads. " + PageContentLimitDescription)] JsonElement? blocks = null,
        [Description("Optional upload id returned by notes_create_upload for larger TipTap blocks JSON or Markdown content. " + PageContentLimitDescription)] string? blocksUploadId = null,
        [Description("Format of blocksUploadId: tiptap_blocks_json or markdown. Markdown is converted server-side into TipTap blocks before append. When omitted, the server infers it from the file name or media type.")] string? blocksFormat = null,
        [Description("Whether to include full contentJson and plainTextContent in the response. Defaults to false to keep write responses small.")] bool includeContent = false)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.AppendBlocksToPage,
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

                var blocksResult = await contentImportService.ResolveRequiredBlocksAsync(
                    pageContextResult.Value.ActorId,
                    blocks,
                    blocksUploadId,
                    blocksFormat,
                    "invalid_blocks",
                    "blocks must be a TipTap block array, or blocksUploadId must reference uploaded Markdown or TipTap blocks JSON.",
                    ct);
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
                JsonElement nextContentJson;
                try
                {
                    nextContentJson = NotesMcpSupport.AppendBlocks(pageContext.Item.ContentJson, blocksResult.Value);
                }
                catch (ArgumentException exception)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_blocks",
                        exception.Message,
                        "blocks",
                        new Dictionary<string, object?>
                        {
                            ["stage"] = "append_blocks"
                        }),
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }
                catch (JsonException exception)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_content_json",
                        $"Existing page content could not be parsed before appending blocks: {exception.Message}",
                        "contentJson",
                        new Dictionary<string, object?>
                        {
                            ["stage"] = "append_existing_content"
                        }),
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var sizeResult = contentImportService.EnforcePageContentSize(nextContentJson, "content_too_large");
                if (!sizeResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        sizeResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        pageContext.Notebook.Id,
                        pageContext.Item.Id,
                        pageContext.ActorId,
                        pageContext.Item.Title,
                        default,
                        null,
                        nextContentJson,
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
                await contentImportService.DeleteUploadAsync(pageContext.ActorId, blocksUploadId, ct);
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Appended blocks to page '{response.Title}'.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ReplaceBlockAtIndex,
        Title = "Replace Block At Index",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Replace one block in a page's TipTap document by its zero-based index in doc.content. " + PageContentLimitDescription + " " + PathCompatibilityDescription)]
    public async Task<CallToolResult> ReplaceBlockAtIndexAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path. " + PathCompatibilityDescription)] string path,
        [Description("Zero-based index of the block to replace in doc.content.")] int index,
        [Description("The new TipTap block JSON object.")] JsonElement block,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("Whether to include full contentJson and plainTextContent in the response. Defaults to false to keep write responses small.")] bool includeContent = false)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.ReplaceBlockAtIndex,
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

                var pageContext = pageContextResult.Value;
                JsonElement nextContentJson;
                try
                {
                    nextContentJson = NotesMcpSupport.ReplaceBlockAtIndex(pageContext.Item.ContentJson, index, block);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "block_index_out_of_range",
                        exception.Message,
                        "index"),
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }
                catch (ArgumentException exception)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_block",
                        exception.Message,
                        "block"),
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var sizeResult = contentImportService.EnforcePageContentSize(nextContentJson, "content_too_large");
                if (!sizeResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        sizeResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        pageContext.Notebook.Id,
                        pageContext.Item.Id,
                        pageContext.ActorId,
                        pageContext.Item.Title,
                        default,
                        null,
                        nextContentJson,
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
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Replaced block at index {index} in page '{response.Title}'.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.InsertBlocksAtIndex,
        Title = "Insert Blocks At Index",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Insert one or more blocks into a page's TipTap document at a zero-based index in doc.content. Use index 0 to insert at the beginning, or doc.content.length to append at the end. " + PageContentLimitDescription + " " + PathCompatibilityDescription)]
    public async Task<CallToolResult> InsertBlocksAtIndexAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path. " + PathCompatibilityDescription)] string path,
        [Description("Zero-based index where the blocks should be inserted in doc.content.")] int index,
        [Description("The TipTap block JSON array to insert.")] JsonElement blocks,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("Whether to include full contentJson and plainTextContent in the response. Defaults to false to keep write responses small.")] bool includeContent = false)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.InsertBlocksAtIndex,
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

                var pageContext = pageContextResult.Value;
                JsonElement nextContentJson;
                try
                {
                    nextContentJson = NotesMcpSupport.InsertBlocksAtIndex(pageContext.Item.ContentJson, index, blocks);
                }
                catch (ArgumentException exception)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_blocks",
                        exception.Message,
                        "blocks"),
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var sizeResult = contentImportService.EnforcePageContentSize(nextContentJson, "content_too_large");
                if (!sizeResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        sizeResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        pageContext.Notebook.Id,
                        pageContext.Item.Id,
                        pageContext.ActorId,
                        pageContext.Item.Title,
                        default,
                        null,
                        nextContentJson,
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
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Inserted {blocks.GetArrayLength()} block(s) at index {index} in page '{response.Title}'.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.DeleteBlockAtIndex,
        Title = "Delete Block At Index",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Delete one block from a page's TipTap document by its zero-based index in doc.content. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> DeleteBlockAtIndexAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path. " + PathCompatibilityDescription)] string path,
        [Description("Zero-based index of the block to delete in doc.content.")] int index,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("Whether to include full contentJson and plainTextContent in the response. Defaults to false to keep write responses small.")] bool includeContent = false)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.DeleteBlockAtIndex,
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

                var pageContext = pageContextResult.Value;
                JsonElement nextContentJson;
                try
                {
                    nextContentJson = NotesMcpSupport.DeleteBlockAtIndex(pageContext.Item.ContentJson, index);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "block_index_out_of_range",
                        exception.Message,
                        "index"),
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var sizeResult = contentImportService.EnforcePageContentSize(nextContentJson, "content_too_large");
                if (!sizeResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        sizeResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        pageContext.Notebook.Id,
                        pageContext.Item.Id,
                        pageContext.ActorId,
                        pageContext.Item.Title,
                        default,
                        null,
                        nextContentJson,
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
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Deleted block at index {index} from page '{response.Title}'.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ReplaceText,
        Title = "Replace Text",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Search and replace plain text inside a page's TipTap document without changing block structure. Only text nodes are modified. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> ReplaceTextAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path. " + PathCompatibilityDescription)] string path,
        [Description("The text to search for.")] string searchText,
        [Description("The text to replace it with.")] string replacementText,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null,
        [Description("Whether to replace all occurrences. Defaults to false (replace only the first occurrence).")] bool replaceAll = false,
        [Description("Whether to include full contentJson and plainTextContent in the response. Defaults to false to keep write responses small.")] bool includeContent = false)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.ReplaceText,
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

                var pageContext = pageContextResult.Value;
                JsonElement nextContentJson;
                try
                {
                    nextContentJson = NotesMcpSupport.ReplaceTextInDocument(pageContext.Item.ContentJson, searchText, replacementText, replaceAll);
                }
                catch (ArgumentException exception)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "text_not_found",
                        exception.Message,
                        "searchText"),
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var sizeResult = contentImportService.EnforcePageContentSize(nextContentJson, "content_too_large");
                if (!sizeResult.Succeeded)
                {
                    return McpMutationResult<UpdatePageContentToolResponse>.Failure(
                        sizeResult.Error!,
                        pageContext.Notebook.Id,
                        pageContext.Item.Id);
                }

                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        pageContext.Notebook.Id,
                        pageContext.Item.Id,
                        pageContext.ActorId,
                        pageContext.Item.Title,
                        default,
                        null,
                        nextContentJson,
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
                return McpMutationResult<UpdatePageContentToolResponse>.Success(
                    response,
                    $"Replaced text in page '{response.Title}'.",
                    pageContext.Notebook.Id,
                    pageContext.Item.Id);
            },
            cancellationToken);
    }
}
