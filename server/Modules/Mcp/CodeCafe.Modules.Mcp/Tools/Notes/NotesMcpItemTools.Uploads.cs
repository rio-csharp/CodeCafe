using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Shared.Application.Common.Interfaces;
using CodeCafe.Shared.Application.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

public sealed partial class NotesMcpItemTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.CreateUpload,
        Title = "Create Upload",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(CreateUploadToolResponse))]
    [Description("Fallback upload path for clients that cannot use the HTTP upload returned by notes_prepare_http_upload. Creates a server-managed MCP upload session for chunked content such as Markdown or TipTap JSON. Use notes_append_upload_chunk to send the file text, then pass the uploadId to notes_create_page, notes_update_page_content, or notes_append_blocks_to_page.")]
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

        var session = await uploadStore.CreateAsync(
            actorResult.Value,
            fileName,
            string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim(),
            cancellationToken);
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
    [Description("Append UTF-8 text to a server-managed upload session. Use this for local Markdown or JSON files instead of assuming shared server storage.")]
    public async Task<CallToolResult> AppendUploadChunk(
        [Description("The upload session id returned by notes_create_upload.")] string uploadId,
        [Description("UTF-8 text chunk to append to the upload. Must not exceed maxUploadChunkBytes returned by notes_get_limits.")] string chunkText,
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

        var appendResult = await uploadStore.AppendTextAsync(
            actorResult.Value,
            uploadId,
            chunkText,
            mcpOptions.MaxUploadChunkBytes,
            mcpOptions.MaxUploadBytes,
            cancellationToken);
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

        var removed = await uploadStore.DeleteAsync(actorResult.Value, uploadId, cancellationToken);
        var response = new DiscardUploadToolResponse(uploadId, removed ? "discarded" : "already_absent");
        await WriteUploadObservationAsync(
            auditService,
            logger,
            actorResult.Value,
            NotesMcpToolNames.DiscardUpload,
            uploadId,
            succeeded: true,
            resultCode: removed ? "success" : "already_absent",
            errorCode: null,
            bytesReceived: null,
            cancellationToken);

        return NotesMcpResultMapper.Success(
            response,
            removed ? $"Upload '{uploadId}' discarded." : $"Upload '{uploadId}' was already absent.");
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
