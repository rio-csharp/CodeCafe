using CodeCafe.Application.Mcp;
using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Identity;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using ModelContextProtocol.Protocol;
using System.Security.Claims;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

public sealed class McpMutationExecutor(
    ApplicationDbContext dbContext,
    IMcpAuditService auditService,
    ILogger<McpMutationExecutor> logger) : IMcpMutationExecutor
{
    public async Task<CallToolResult> ExecuteAsync<T>(
        ClaimsPrincipal user,
        string toolName,
        Func<CancellationToken, Task<McpMutationResult<T>>> operation,
        CancellationToken cancellationToken)
        where T : class
    {
        var startedTransaction = dbContext.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = startedTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var result = await operation(cancellationToken);
            var auditRecord = CreateAuditRecord(user, toolName, result);

            if (!result.Succeeded)
            {
                if (transaction is not null)
                {
                    // Roll back without the request token: when the client disconnects
                    // mid-mutation the rollback must still run so the failure response
                    // and the failure audit entry below are not lost.
                    transaction = await RollbackAndDisposeAsync(transaction, CancellationToken.None);
                }

                dbContext.ChangeTracker.Clear();
                await TryWriteFailureAuditAsync(auditRecord, CancellationToken.None);
                return NotesMcpResultMapper.Failure(result.Error!);
            }

            await auditService.WriteAsync(auditRecord, cancellationToken);

            if (transaction is not null)
            {
                transaction = await CommitAndDisposeAsync(transaction, cancellationToken);
            }

            return NotesMcpResultMapper.Success(result.Value!, result.SuccessText!);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "MCP mutation execution failed for tool {ToolName}.", toolName);

            transaction = await TryRollbackAndDisposeAsync(transaction, toolName);

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Builds the audit record for a mutation. The actor is read from the tool's own
    /// <see cref="ClaimsPrincipal"/> rather than an ambient accessor: the MCP transport is what
    /// authenticated this call, and the calling tool has already gone through
    /// <c>NotesMcpSupport.RequireActor</c>, so the principal is the authoritative actor here.
    /// </summary>
    private static McpAuditRecord CreateAuditRecord<T>(
        ClaimsPrincipal user,
        string toolName,
        McpMutationResult<T> result)
        where T : class
        => new(
            CurrentUserClaims.GetUserId(user) ?? Guid.Empty,
            "user",
            toolName,
            result.NotebookId,
            result.ItemId,
            result.Succeeded,
            result.Succeeded ? "success" : result.Error!.Code,
            result.Error?.Code);

    private async Task TryWriteFailureAuditAsync(McpAuditRecord auditRecord, CancellationToken cancellationToken)
    {
        try
        {
            await auditService.WriteIndependentAsync(auditRecord, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to write MCP failure audit entry for tool {ToolName}.",
                auditRecord.ToolName);
        }
    }

    private static async Task<IDbContextTransaction?> CommitAndDisposeAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();
        return null;
    }

    private static async Task<IDbContextTransaction?> RollbackAndDisposeAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        await transaction.DisposeAsync();
        return null;
    }

    private async Task<IDbContextTransaction?> TryRollbackAndDisposeAsync(
        IDbContextTransaction? transaction,
        string toolName)
    {
        if (transaction is null)
        {
            return null;
        }

        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception rollbackException)
        {
            logger.LogWarning(
                rollbackException,
                "Failed to roll back MCP mutation transaction for tool {ToolName}.",
                toolName);
        }
        finally
        {
            await transaction.DisposeAsync();
        }

        return null;
    }
}
