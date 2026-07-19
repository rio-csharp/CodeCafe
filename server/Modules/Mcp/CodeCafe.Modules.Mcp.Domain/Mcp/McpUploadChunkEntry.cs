using CodeCafe.Shared.Domain.Common.Interfaces;

namespace CodeCafe.Shared.Domain.Mcp;

public sealed class McpUploadChunkEntry : IAuditableEntity
{
    public Guid Id { get; set; }

    public required string UploadId { get; set; }

    public McpUploadSessionEntry UploadSession { get; set; } = null!;

    public int SequenceNumber { get; set; }

    public required string ContentText { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
