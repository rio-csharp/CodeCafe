namespace CodeCafe.Host.Mcp;

internal sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public bool Enabled { get; set; } = true;

    public string EndpointPath { get; set; } = "/mcp";
}
