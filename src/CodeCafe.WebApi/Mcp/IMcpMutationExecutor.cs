using ModelContextProtocol.Protocol;
using System.Security.Claims;

namespace CodeCafe.WebApi.Mcp;

public interface IMcpMutationExecutor
{
    Task<CallToolResult> ExecuteAsync<T>(
        ClaimsPrincipal user,
        string toolName,
        Func<CancellationToken, Task<McpMutationResult<T>>> operation,
        CancellationToken cancellationToken)
        where T : class;
}
