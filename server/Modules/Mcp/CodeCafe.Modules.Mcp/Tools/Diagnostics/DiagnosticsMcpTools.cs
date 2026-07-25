using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace CodeCafe.Modules.Mcp.Tools.Diagnostics;

[McpServerToolType]
public sealed class DiagnosticsMcpTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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
            StructuredContent = JsonSerializer.SerializeToElement(payload, SerializerOptions)
        };
    }
}

public sealed record DiagnosticsStatusResponse(string Status, string Adapter);
