namespace CodeCafe.Application.Common.Interfaces;

public interface IMcpAuditService
{
    Task WriteAsync(
        Guid? actorUserId,
        string actorType,
        string toolName,
        Guid? notebookId,
        Guid? itemId,
        bool succeeded,
        string resultCode,
        string? errorCode,
        CancellationToken cancellationToken);
}
