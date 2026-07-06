namespace CodeCafe.Application.Common.Interfaces;

public interface IMcpAuditService
{
    Task WriteAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken);

    Task WriteIndependentAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken);
}
