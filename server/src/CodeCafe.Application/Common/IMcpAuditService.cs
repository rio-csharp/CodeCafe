namespace CodeCafe.Application.Common;

public interface IMcpAuditService
{
    Task WriteAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken);

    Task WriteIndependentAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken);
}
