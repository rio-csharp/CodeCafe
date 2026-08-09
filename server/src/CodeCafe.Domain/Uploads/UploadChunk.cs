using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Uploads;

/// <summary>
/// Represents a single chunk of an upload session. Chunks are stored with sequence
/// numbers to preserve order and support concurrent appends.
/// </summary>
public sealed class UploadChunk : IAuditableEntity
{
    public Guid Id { get; set; }

    public required string UploadId { get; set; }

    public UploadSession UploadSession { get; set; } = null!;

    public int SequenceNumber { get; set; }

    public required string ContentText { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
