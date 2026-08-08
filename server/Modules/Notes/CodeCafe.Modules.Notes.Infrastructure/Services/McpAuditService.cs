using CodeCafe.Shared.Application.Common.Interfaces;
using CodeCafe.Domain.Mcp;
using CodeCafe.Shared.Infrastructure.Persistence;

namespace CodeCafe.Modules.Notes.Infrastructure.Services;

internal sealed class McpAuditService(
    ApplicationDbContext dbContext,
    IMcpIndependentAuditQueue independentAuditQueue) : IMcpAuditService
{
    public Task WriteAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken)
        => WriteEntryAsync(dbContext, auditRecord, cancellationToken);

    public Task WriteIndependentAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken)
        => independentAuditQueue.EnqueueAsync(auditRecord, cancellationToken).AsTask();

    private static async Task WriteEntryAsync(
        ApplicationDbContext dbContext,
        McpAuditRecord auditRecord,
        CancellationToken cancellationToken)
    {
        dbContext.McpToolAuditEntries.Add(new McpToolAuditEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = auditRecord.ActorUserId,
            ActorType = auditRecord.ActorType,
            ToolName = auditRecord.ToolName,
            NotebookId = auditRecord.NotebookId,
            ItemId = auditRecord.ItemId,
            Succeeded = auditRecord.Succeeded,
            ResultCode = auditRecord.ResultCode,
            ErrorCode = auditRecord.ErrorCode
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
