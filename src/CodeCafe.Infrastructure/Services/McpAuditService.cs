using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Domain.Mcp;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure.Services;

public sealed class McpAuditService(
    ApplicationDbContext dbContext,
    IServiceScopeFactory serviceScopeFactory) : IMcpAuditService
{
    public Task WriteAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken)
        => WriteEntryAsync(dbContext, auditRecord, cancellationToken);

    public async Task WriteIndependentAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await WriteEntryAsync(scopedDbContext, auditRecord, cancellationToken);
    }

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
