namespace CodeCafe.Application.Common.Interfaces;

public sealed record McpAuditRecord(
    Guid? ActorUserId,
    string ActorType,
    string ToolName,
    Guid? NotebookId,
    Guid? ItemId,
    bool Succeeded,
    string ResultCode,
    string? ErrorCode);
