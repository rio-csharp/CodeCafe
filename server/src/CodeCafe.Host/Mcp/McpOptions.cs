namespace CodeCafe.Host.Mcp;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public bool Enabled { get; set; } = true;

    public string EndpointPath { get; set; } = "/mcp";

    // Markdown a client hands over as a tool argument has to be read and re-emitted by the
    // model, so every byte costs tokens twice. Past this size the import tools refuse and
    // return the HTTP recipe instead. The cap is enforced rather than documented: a rule that
    // lives only in a tool description is advice the model can skip.
    public int MaxInlineContentBytes { get; set; } = 32 * 1024;
}
