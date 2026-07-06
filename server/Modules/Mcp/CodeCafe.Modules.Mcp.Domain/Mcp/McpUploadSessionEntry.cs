using CodeCafe.Domain.Common.Interfaces;

namespace CodeCafe.Domain.Mcp;

public sealed class McpUploadSessionEntry : IAuditableEntity
{
    public required string UploadId { get; set; }

    public Guid ActorUserId { get; set; }

    public string? FileName { get; set; }

    public required string MediaType { get; set; }

    public int BytesReceived { get; set; }

    public int ChunkCount { get; set; }

    public List<McpUploadChunkEntry> Chunks { get; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
