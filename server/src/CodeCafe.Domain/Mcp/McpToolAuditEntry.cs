using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Mcp;

public sealed class McpToolAuditEntry : Entity, IAuditableEntity
{
    private McpToolAuditEntry() { }

    private McpToolAuditEntry(
        Guid id,
        Guid actorUserId,
        string actorType,
        string toolName,
        Guid? notebookId,
        Guid? itemId,
        bool succeeded,
        string resultCode,
        string? errorCode,
        DateTimeOffset createdAtUtc
    )
    {
        Id = id;
        ActorUserId = actorUserId;
        ActorType = actorType;
        ToolName = toolName;
        NotebookId = notebookId;
        ItemId = itemId;
        Succeeded = succeeded;
        ResultCode = resultCode;
        ErrorCode = errorCode;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ActorUserId { get; private set; }

    public string ActorType { get; private set; } = null!;

    public string ToolName { get; private set; } = null!;

    public Guid? NotebookId { get; private set; }

    public Guid? ItemId { get; private set; }

    public bool Succeeded { get; private set; }

    public string ResultCode { get; private set; } = null!;

    public string? ErrorCode { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static McpToolAuditEntry Create(
        Guid id,
        Guid actorUserId,
        string actorType,
        string toolName,
        Guid? notebookId,
        Guid? itemId,
        bool succeeded,
        string resultCode,
        string? errorCode,
        DateTimeOffset now
    ) =>
        new(
            id,
            actorUserId,
            actorType,
            toolName,
            notebookId,
            itemId,
            succeeded,
            resultCode,
            errorCode,
            now
        );
}
