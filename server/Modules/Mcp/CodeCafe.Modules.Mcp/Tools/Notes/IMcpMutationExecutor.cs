using ModelContextProtocol.Protocol;
using System.Security.Claims;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

public interface IMcpMutationExecutor
{
    Task<CallToolResult> ExecuteAsync<T>(
        ClaimsPrincipal user,
        string toolName,
        Func<CancellationToken, Task<McpMutationResult<T>>> operation,
        CancellationToken cancellationToken)
        where T : class;
}
