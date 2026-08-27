using System.Security.Claims;
using CodeCafe.Application.Mcp;
using ModelContextProtocol.Protocol;

namespace CodeCafe.Host.Mcp;

public interface IMcpMutationExecutor
{
    Task<CallToolResult> ExecuteAsync<T>(
        ClaimsPrincipal user,
        string toolName,
        Func<CancellationToken, Task<McpMutationResult<T>>> operation,
        CancellationToken cancellationToken
    )
        where T : class;
}
