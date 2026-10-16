using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Options;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class ImportMcpTools
{
    // Cheap on purpose: the result is a few hundred bytes, and calling it saves a client from
    // pushing a whole notebook through the model to find out how.
    [McpServerTool(Name = "codecafe_get_import_recipe", ReadOnly = true, Idempotent = true)]
    [Description(
        "Get the HTTP request details for importing markdown. Use this instead of passing a large "
            + "payload to codecafe_import_notebook or codecafe_import_page: content sent over HTTP "
            + "with the client's own bearer token never enters the model's context. Returns the "
            + "endpoint URLs, the authentication header, the size limits, and a ready-to-run example."
    )]
    public static CallToolResult GetImportRecipe(
        IHttpContextAccessor httpContextAccessor,
        IOptions<McpOptions> mcpOptions
    ) => McpToolResults.Success(ImportGuidance.Recipe(httpContextAccessor.HttpContext, mcpOptions.Value));
}
