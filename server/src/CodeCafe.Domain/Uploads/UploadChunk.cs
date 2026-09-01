using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Uploads;

public sealed class UploadChunk : Entity, IAuditableEntity
{
    private UploadChunk() { }

    private UploadChunk(
        Guid id,
        Guid uploadId,
        int sequenceNumber,
        string contentText,
        DateTimeOffset createdAtUtc
    )
    {
        Id = id;
        UploadId = uploadId;
        SequenceNumber = sequenceNumber;
        ContentText = contentText;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UploadId { get; private set; }

    public int SequenceNumber { get; private set; }

    public string ContentText { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    internal static UploadChunk Create(
        Guid id,
        Guid uploadId,
        int sequenceNumber,
        string contentText,
        DateTimeOffset createdAtUtc
    ) => new(id, uploadId, sequenceNumber, contentText, createdAtUtc);
}
