using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Domain.Mcp;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace CodeCafe.Infrastructure.Services;

internal interface IMcpIndependentAuditQueue
{
    ValueTask EnqueueAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken);
}

internal sealed class McpIndependentAuditQueue(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<McpIndependentAuditQueue> logger) : BackgroundService, IMcpIndependentAuditQueue
{
    private const int MaxBatchSize = 32;
    private readonly Channel<McpAuditRecord> queue = Channel.CreateUnbounded<McpAuditRecord>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken)
        => queue.Writer.WriteAsync(auditRecord, cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await queue.Reader.WaitToReadAsync(stoppingToken))
        {
            var batch = new List<McpAuditRecord>(MaxBatchSize);
            while (batch.Count < MaxBatchSize && queue.Reader.TryRead(out var auditRecord))
            {
                batch.Add(auditRecord);
            }

            if (batch.Count == 0)
            {
                continue;
            }

            try
            {
                await FlushBatchAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to flush {Count} queued MCP audit record(s).", batch.Count);
            }
        }
    }

    private async Task FlushBatchAsync(IReadOnlyList<McpAuditRecord> batch, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var auditRecord in batch)
        {
            dbContext.McpToolAuditEntries.Add(CreateAuditEntry(auditRecord));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static McpToolAuditEntry CreateAuditEntry(McpAuditRecord auditRecord)
    {
        return new McpToolAuditEntry
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
        };
    }
}
