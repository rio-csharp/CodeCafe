using System.Text;
using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Uploads;

public sealed class UploadSession : Entity, IAuditableEntity
{
    private readonly List<UploadChunk> _chunks = [];

    private UploadSession() { }

    private UploadSession(
        Guid id,
        Guid actorUserId,
        string? fileName,
        string mediaType,
        DateTimeOffset createdAtUtc
    )
    {
        Id = id;
        ActorUserId = actorUserId;
        FileName = fileName;
        MediaType = mediaType;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ActorUserId { get; private set; }

    public string? FileName { get; private set; }

    public string MediaType { get; private set; } = null!;

    public int BytesReceived { get; private set; }

    public int ChunkCount { get; private set; }

    public IReadOnlyCollection<UploadChunk> Chunks => _chunks.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static UploadSession Create(
        Guid id,
        Guid actorUserId,
        string? fileName,
        string mediaType,
        DateTimeOffset now
    ) => new(id, actorUserId, fileName, mediaType, now);

    public UploadChunk AppendChunk(string contentText, DateTimeOffset now)
    {
        var chunk = UploadChunk.Create(Guid.NewGuid(), Id, ChunkCount + 1, contentText, now);
        _chunks.Add(chunk);
        ChunkCount++;
        BytesReceived += Encoding.UTF8.GetByteCount(contentText);
        return chunk;
    }
}
