using CodeCafe.Application.Common.Configuration;
using CodeCafe.Application.Common.Uploads;
using CodeCafe.Domain.Uploads;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

using DomainUploadSession = CodeCafe.Domain.Uploads.UploadSession;
using DomainUploadChunk = CodeCafe.Domain.Uploads.UploadChunk;

namespace CodeCafe.Infrastructure.Uploads;

public sealed class DatabaseUploadStore(
    ApplicationDbContext dbContext,
    IOptions<McpOptions> mcpOptionsAccessor) : IUploadStore
{
    public async Task<UploadStatus> CreateAsync(
        Guid actorId,
        string? fileName,
        string mediaType,
        CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var session = new DomainUploadSession
        {
            UploadId = Guid.NewGuid().ToString("N"),
            ActorUserId = actorId,
            FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            MediaType = string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim(),
            BytesReceived = 0,
            ChunkCount = 0
        };

        dbContext.UploadSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToStatus(session);
    }

    /// <remarks>
    /// Concurrent appends to one upload race twice over: the sequence number is derived from the
    /// session's chunk count, and (UploadId, SequenceNumber) is unique, so two writers pick the same
    /// number and one insert fails; and BytesReceived is a read-modify-write, so the loser's bytes
    /// would go missing. Each attempt therefore re-reads the session and derives the next sequence
    /// number from the chunks already stored, and a duplicate-key loss is retried on that fresh state.
    /// </remarks>
    public async Task<UploadResult<UploadStatus>> AppendTextAsync(
        Guid actorId,
        string uploadId,
        string chunkText,
        int maxChunkBytes,
        int maxUploadBytes,
        CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var chunkBytes = Encoding.UTF8.GetByteCount(chunkText);
        if (chunkBytes == 0)
        {
            return UploadResult<UploadStatus>.Failure("invalid_upload_chunk", "Upload chunk text is required.");
        }

        if (chunkBytes > maxChunkBytes)
        {
            return UploadResult<UploadStatus>.Failure(
                "upload_chunk_too_large",
                $"Upload chunk exceeds the limit of {maxChunkBytes} bytes (received {chunkBytes} bytes).");
        }

        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            // Detach anything a previous attempt tracked so the retry starts from committed state.
            dbContext.ChangeTracker.Clear();

            var session = await dbContext.UploadSessions
                .SingleOrDefaultAsync(existingSession => existingSession.UploadId == uploadId, cancellationToken);
            if (session is null || session.ActorUserId != actorId)
            {
                return UploadResult<UploadStatus>.Failure("upload_not_found", "Upload session was not found.");
            }

            var nextBytes = session.BytesReceived + chunkBytes;
            if (nextBytes > maxUploadBytes)
            {
                return UploadResult<UploadStatus>.Failure(
                    "upload_too_large",
                    $"Upload exceeds the limit of {maxUploadBytes} bytes (received {nextBytes} bytes).");
            }

            // Derived from the stored chunks rather than ChunkCount so a session counter that drifted
            // (or a concurrent insert) cannot hand out a number that is already taken.
            var highestSequence = await dbContext.UploadChunks
                .Where(chunk => chunk.UploadId == uploadId)
                .MaxAsync(chunk => (int?)chunk.SequenceNumber, cancellationToken) ?? 0;

            dbContext.UploadChunks.Add(new DomainUploadChunk
            {
                Id = Guid.NewGuid(),
                UploadId = session.UploadId,
                SequenceNumber = highestSequence + 1,
                ContentText = chunkText
            });

            session.BytesReceived = nextBytes;
            session.ChunkCount += 1;

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return UploadResult<UploadStatus>.Success(ToStatus(session));
            }
            catch (DbUpdateException exception) when (
                attempt < maxAttempts && IsDuplicateChunkSequenceException(exception))
            {
                // Another append took this sequence number first; retry against the new state.
            }
        }
    }

    /// <summary>
    /// Detects a violation of the unique (UploadId, SequenceNumber) index on UploadChunks.
    /// </summary>
    private static bool IsDuplicateChunkSequenceException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        // Check both old and new index names for compatibility during migration
        return message.Contains("IX_UploadChunks_UploadId_SequenceNumber", StringComparison.OrdinalIgnoreCase)
               || message.Contains("IX_McpUploadChunks_UploadId_SequenceNumber", StringComparison.OrdinalIgnoreCase)
               || (message.Contains("UploadChunks", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("SequenceNumber", StringComparison.OrdinalIgnoreCase)
                   && (message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                       || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<UploadResult<UploadStatus>> CreateTextAsync(
        Guid actorId,
        string? fileName,
        string mediaType,
        string contentText,
        int maxUploadBytes,
        CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var normalizedText = contentText ?? string.Empty;
        var contentBytes = Encoding.UTF8.GetByteCount(normalizedText);
        if (contentBytes == 0)
        {
            return UploadResult<UploadStatus>.Failure("invalid_upload_chunk", "Upload content is required.");
        }

        if (contentBytes > maxUploadBytes)
        {
            return UploadResult<UploadStatus>.Failure(
                "upload_too_large",
                $"Upload exceeds the limit of {maxUploadBytes} bytes (received {contentBytes} bytes).");
        }

        var session = new DomainUploadSession
        {
            UploadId = Guid.NewGuid().ToString("N"),
            ActorUserId = actorId,
            FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
            MediaType = string.IsNullOrWhiteSpace(mediaType) ? "text/plain" : mediaType.Trim(),
            BytesReceived = contentBytes,
            ChunkCount = 1
        };

        dbContext.UploadSessions.Add(session);
        dbContext.UploadChunks.Add(new DomainUploadChunk
        {
            Id = Guid.NewGuid(),
            UploadId = session.UploadId,
            SequenceNumber = 1,
            ContentText = normalizedText
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return UploadResult<UploadStatus>.Success(ToStatus(session));
    }

    public async Task<UploadResult<Application.Common.Uploads.UploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken)
    {
        await PruneExpiredUploadsAsync(cancellationToken);

        var session = await dbContext.UploadSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(existingSession => existingSession.UploadId == uploadId, cancellationToken);
        if (session is null || session.ActorUserId != actorId)
        {
            return UploadResult<Application.Common.Uploads.UploadSession>.Failure("upload_not_found", "Upload session was not found.");
        }

        var chunks = await dbContext.UploadChunks
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

        return UploadResult<Application.Common.Uploads.UploadSession>.Success(new Application.Common.Uploads.UploadSession(
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

        var session = await dbContext.UploadSessions
            .SingleOrDefaultAsync(existingSession => existingSession.UploadId == uploadId, cancellationToken);
        if (session is null || session.ActorUserId != actorId)
        {
            return false;
        }

        dbContext.UploadSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task PruneExpiredUploadsAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(mcpOptionsAccessor.Value.UploadIdleTimeoutSeconds);
        var cutoff = DateTimeOffset.UtcNow - timeout;

        await dbContext.UploadSessions
            .Where(session => (session.UpdatedAtUtc ?? session.CreatedAtUtc) <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static UploadStatus ToStatus(DomainUploadSession session)
    {
        return new UploadStatus(
            session.UploadId,
            session.ActorUserId,
            session.FileName,
            session.MediaType,
            session.BytesReceived,
            session.CreatedAtUtc,
            session.UpdatedAtUtc ?? session.CreatedAtUtc);
    }
}
