using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Domain.Mcp;
using CodeCafe.Infrastructure.Persistence;

namespace CodeCafe.Infrastructure.Services;

public sealed class McpAuditService(ApplicationDbContext dbContext) : IMcpAuditService
{
    public async Task WriteAsync(
        Guid? actorUserId,
        string actorType,
        string toolName,
        Guid? notebookId,
        Guid? itemId,
        bool succeeded,
        string resultCode,
        string? errorCode,
        CancellationToken cancellationToken)
    {
        dbContext.McpToolAuditEntries.Add(new McpToolAuditEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorType = actorType,
            ToolName = toolName,
            NotebookId = notebookId,
            ItemId = itemId,
            Succeeded = succeeded,
            ResultCode = resultCode,
            ErrorCode = errorCode
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
