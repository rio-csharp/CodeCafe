using ModelContextProtocol.Server;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

[McpServerToolType]
public sealed partial class NotesMcpItemTools
{
    private const string PathCompatibilityDescription = "Use the path returned by MCP responses. Resource-style page/<path> and folder/<path> inputs are also accepted for clients that derive paths from item resource URIs.";
    private const string PageContentLimitDescription = "Default limits: maxInlineContentBytes=131072, maxPageContentBytes=1048576, maxTipTapDepth=64, maxTipTapNodeCount=5000, maxTipTapTextLength=200000. Runtime values are returned by notes_get_limits.";
}
