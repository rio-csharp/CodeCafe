using CodeCafe.Domain.Mcp;
using CodeCafe.Infrastructure.Persistence;
using CodeCafe.Mcp.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace CodeCafe.Mcp.Tools.Notes;

public interface IMcpUploadStore
{
    Task<McpUploadStatus> CreateAsync(Guid actorId, string? fileName, string mediaType, CancellationToken cancellationToken);

    Task<NotesUploadResult<McpUploadStatus>> AppendTextAsync(
        Guid actorId,
        string uploadId,
        string chunkText,
        int maxChunkBytes,
        int maxUploadBytes,
        CancellationToken cancellationToken);

    Task<NotesUploadResult<McpUploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid actorId, string uploadId, CancellationToken cancellationToken);
}

public sealed record McpUploadStatus(
    string UploadId,
    Guid ActorId,
    string? FileName,
    string MediaType,
    int BytesReceived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record McpUploadSession(
    string UploadId,
    Guid ActorId,
    string? FileName,
    string MediaType,
    string ContentText,
    int BytesReceived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record NotesUploadError(string Code, string Message);

public sealed record NotesUploadResult<T>(T? Value, NotesUploadError? Error)
{
    public bool Succeeded => Error is null;

    public static NotesUploadResult<T> Success(T value) => new(value, null);

    public static NotesUploadResult<T> Failure(string code, string message) => new(default, new NotesUploadError(code, message));
}

public sealed class DatabaseMcpUploadStore(
    ApplicationDbContext dbContext,
    IOptions<McpOptions> mcpOptionsAccessor) : IMcpUploadStore
{
    public async Task<McpUploadStatus> CreateAsync(
        Guid actorId,
        string? fileName,
        string mediaType,
        CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var session = new McpUploadSessionEntry
        {
            UploadId = Guid.NewGuid().ToString("N"),
            ActorUserId = actorId,
            FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            MediaType = string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim(),
            BytesReceived = 0,
            ChunkCount = 0
        };

        dbContext.McpUploadSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToStatus(session);
    }

    public async Task<NotesUploadResult<McpUploadStatus>> AppendTextAsync(
        Guid actorId,
        string uploadId,
        string chunkText,
        int maxChunkBytes,
        int maxUploadBytes,
        CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var session = await dbContext.McpUploadSessions
            .SingleOrDefaultAsync(existingSession => existingSession.UploadId == uploadId, cancellationToken);
        if (session is null || session.ActorUserId != actorId)
        {
            return NotesUploadResult<McpUploadStatus>.Failure("upload_not_found", "Upload session was not found.");
        }

        var chunkBytes = Encoding.UTF8.GetByteCount(chunkText);
        if (chunkBytes == 0)
        {
            return NotesUploadResult<McpUploadStatus>.Failure("invalid_upload_chunk", "Upload chunk text is required.");
        }

        if (chunkBytes > maxChunkBytes)
        {
            return NotesUploadResult<McpUploadStatus>.Failure(
                "upload_chunk_too_large",
                $"Upload chunk exceeds the limit of {maxChunkBytes} bytes (received {chunkBytes} bytes).");
        }

        var nextBytes = session.BytesReceived + chunkBytes;
        if (nextBytes > maxUploadBytes)
        {
            return NotesUploadResult<McpUploadStatus>.Failure(
                "upload_too_large",
                $"Upload exceeds the limit of {maxUploadBytes} bytes (received {nextBytes} bytes).");
        }

        dbContext.McpUploadChunks.Add(new McpUploadChunkEntry
        {
            Id = Guid.NewGuid(),
            UploadId = session.UploadId,
            SequenceNumber = session.ChunkCount + 1,
            ContentText = chunkText
        });

        session.BytesReceived = nextBytes;
        session.ChunkCount += 1;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NotesUploadResult<McpUploadStatus>.Success(ToStatus(session));
    }

    public async Task<NotesUploadResult<McpUploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var session = await dbContext.McpUploadSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(existingSession => existingSession.UploadId == uploadId, cancellationToken);
        if (session is null || session.ActorUserId != actorId)
        {
            return NotesUploadResult<McpUploadSession>.Failure("upload_not_found", "Upload session was not found.");
        }

        var chunks = await dbContext.McpUploadChunks
            .AsNoTracking()
            .Where(chunk => chunk.UploadId == uploadId)
            .OrderBy(chunk => chunk.SequenceNumber)
            .Select(chunk => chunk.ContentText)
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder(session.BytesReceived);
        foreach (var chunk in chunks)
        {
            builder.Append(chunk);
        }

        return NotesUploadResult<McpUploadSession>.Success(new McpUploadSession(
            session.UploadId,
            session.ActorUserId,
            session.FileName,
            session.MediaType,
            builder.ToString(),
            session.BytesReceived,
            session.CreatedAtUtc,
            session.UpdatedAtUtc ?? session.CreatedAtUtc));
    }

    public async Task<bool> DeleteAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var session = await dbContext.McpUploadSessions
            .SingleOrDefaultAsync(existingSession => existingSession.UploadId == uploadId, cancellationToken);
        if (session is null || session.ActorUserId != actorId)
        {
            return false;
        }

        dbContext.McpUploadSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task PruneExpiredUploadsAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(mcpOptionsAccessor.Value.UploadIdleTimeoutSeconds);
        var cutoff = DateTimeOffset.UtcNow - timeout;

        await dbContext.McpUploadSessions
            .Where(session => (session.UpdatedAtUtc ?? session.CreatedAtUtc) <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static McpUploadStatus ToStatus(McpUploadSessionEntry session)
    {
        return new McpUploadStatus(
            session.UploadId,
            session.ActorUserId,
            session.FileName,
            session.MediaType,
            session.BytesReceived,
            session.CreatedAtUtc,
            session.UpdatedAtUtc ?? session.CreatedAtUtc);
    }
}
