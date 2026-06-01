using CodeCafe.Mcp.Configuration;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;

namespace CodeCafe.Mcp.Tools.Notes;

public interface IMcpUploadStore
{
    McpUploadSession Create(Guid actorId, string? fileName, string mediaType);

    NotesUploadResult<McpUploadSession> AppendText(Guid actorId, string uploadId, string chunkText, int maxChunkBytes, int maxUploadBytes);

    NotesUploadResult<McpUploadSession> Get(Guid actorId, string uploadId);

    bool Delete(Guid actorId, string uploadId);
}

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

public sealed class InMemoryMcpUploadStore(IOptions<McpOptions> mcpOptionsAccessor) : IMcpUploadStore
{
    private readonly ConcurrentDictionary<string, UploadBuffer> uploads = new(StringComparer.Ordinal);
    private readonly TimeProvider timeProvider = TimeProvider.System;

    public McpUploadSession Create(Guid actorId, string? fileName, string mediaType)
    {
        PruneExpiredUploads();

        var now = timeProvider.GetUtcNow();
        var session = new McpUploadSession(
            Guid.NewGuid().ToString("N"),
            actorId,
            string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim(),
            string.Empty,
            0,
            now,
            now);

        uploads[session.UploadId] = new UploadBuffer(session);
        return session;
    }

    public NotesUploadResult<McpUploadSession> AppendText(Guid actorId, string uploadId, string chunkText, int maxChunkBytes, int maxUploadBytes)
    {
        PruneExpiredUploads();

        if (!uploads.TryGetValue(uploadId, out var buffer))
        {
            return NotesUploadResult<McpUploadSession>.Failure("upload_not_found", "Upload session was not found.");
        }

        if (buffer.Session.ActorId != actorId)
        {
            return NotesUploadResult<McpUploadSession>.Failure("upload_not_found", "Upload session was not found.");
        }

        var chunkBytes = Encoding.UTF8.GetByteCount(chunkText);
        if (chunkBytes == 0)
        {
            return NotesUploadResult<McpUploadSession>.Failure("invalid_upload_chunk", "Upload chunk text is required.");
        }

        if (chunkBytes > maxChunkBytes)
        {
            return NotesUploadResult<McpUploadSession>.Failure(
                "upload_chunk_too_large",
                $"Upload chunk exceeds the limit of {maxChunkBytes} bytes (received {chunkBytes} bytes).");
        }

        lock (buffer.SyncRoot)
        {
            var nextBytes = buffer.Session.BytesReceived + chunkBytes;
            if (nextBytes > maxUploadBytes)
            {
                return NotesUploadResult<McpUploadSession>.Failure(
                    "upload_too_large",
                    $"Upload exceeds the limit of {maxUploadBytes} bytes (received {nextBytes} bytes).");
            }

            buffer.Builder.Append(chunkText);
            var now = timeProvider.GetUtcNow();
            buffer.Session = buffer.Session with
            {
                BytesReceived = nextBytes,
                UpdatedAtUtc = now
            };
            return NotesUploadResult<McpUploadSession>.Success(CreateSnapshot(buffer));
        }
    }

    public NotesUploadResult<McpUploadSession> Get(Guid actorId, string uploadId)
    {
        PruneExpiredUploads();

        if (!uploads.TryGetValue(uploadId, out var buffer) || buffer.Session.ActorId != actorId)
        {
            return NotesUploadResult<McpUploadSession>.Failure("upload_not_found", "Upload session was not found.");
        }

        lock (buffer.SyncRoot)
        {
            return NotesUploadResult<McpUploadSession>.Success(CreateSnapshot(buffer));
        }
    }

    public bool Delete(Guid actorId, string uploadId)
    {
        PruneExpiredUploads();

        if (!uploads.TryGetValue(uploadId, out var buffer) || buffer.Session.ActorId != actorId)
        {
            return false;
        }

        return uploads.TryRemove(uploadId, out _);
    }

    private void PruneExpiredUploads()
    {
        var timeout = TimeSpan.FromSeconds(mcpOptionsAccessor.Value.UploadIdleTimeoutSeconds);
        var cutoff = timeProvider.GetUtcNow() - timeout;

        foreach (var pair in uploads)
        {
            if (pair.Value.Session.UpdatedAtUtc <= cutoff)
            {
                uploads.TryRemove(pair.Key, out _);
            }
        }
    }

    private static McpUploadSession CreateSnapshot(UploadBuffer buffer)
    {
        return buffer.Session with
        {
            ContentText = buffer.Builder.ToString()
        };
    }

    private sealed class UploadBuffer
    {
        public UploadBuffer(McpUploadSession session)
        {
            Session = session;
        }

        public object SyncRoot { get; } = new();

        public StringBuilder Builder { get; } = new();

        public McpUploadSession Session { get; set; }
    }
}
