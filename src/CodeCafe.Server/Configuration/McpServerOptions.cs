namespace CodeCafe.Server.Configuration;

public sealed class McpServerOptions
{
    public const string SectionName = "Mcp";

    public bool Enabled { get; set; }

    public string EndpointPath { get; set; } = "/mcp";

    public string[] AllowedOrigins { get; set; } = [];

    public bool RequireAuthorization { get; set; } = true;

    public string RequiredAudience { get; set; } = "codecafe-mcp";

    public string ProtectedResourceMetadataPath { get; set; } = "/.well-known/oauth-protected-resource/mcp";

    public string[] RequiredReadScopes { get; set; } = ["notes.read"];

    public string[] RequiredWriteScopes { get; set; } = ["notes.write"];
}
