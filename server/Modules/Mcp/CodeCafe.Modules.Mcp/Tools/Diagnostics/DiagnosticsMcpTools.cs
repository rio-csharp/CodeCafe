using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CodeCafe.Mcp.Tools.Diagnostics;

[McpServerToolType]
public sealed class DiagnosticsMcpTools
{
    [McpServerTool(
        Name = "diagnostics_status",
        Title = "Diagnostics Status",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(DiagnosticsStatusResponse))]
    [Description("Return the current MCP adapter status for diagnostics and smoke testing.")]
    public CallToolResult GetStatus()
    {
        var payload = new DiagnosticsStatusResponse("ok", "mcp");
        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = "CodeCafe MCP adapter is healthy."
                }
            ],
            StructuredContent = System.Text.Json.JsonSerializer.SerializeToElement(payload)
        };
    }
}

public sealed record DiagnosticsStatusResponse(string Status, string Adapter);
