using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Uploads;

/// <summary>
/// Represents a content upload session. Supports chunked uploads for large files.
/// Originally part of the MCP module, now a general-purpose upload mechanism.
/// </summary>
public sealed class UploadSession : IAuditableEntity
{
    public required string UploadId { get; set; }

    public Guid ActorUserId { get; set; }

    public string? FileName { get; set; }

    public required string MediaType { get; set; }

    public int BytesReceived { get; set; }

    public int ChunkCount { get; set; }

    public List<UploadChunk> Chunks { get; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
