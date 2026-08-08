using CodeCafe.Shared.Domain.Common.Interfaces;

namespace CodeCafe.Shared.Domain.Mcp;

public sealed class McpToolAuditEntry : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public required string ActorType { get; set; }

    public required string ToolName { get; set; }

    public Guid? NotebookId { get; set; }

    public Guid? ItemId { get; set; }

    public bool Succeeded { get; set; }

    public required string ResultCode { get; set; }

    public string? ErrorCode { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
