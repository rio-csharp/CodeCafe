namespace CodeCafe.WebApi.Mcp;

public sealed class McpOptions
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

    public int MaxInlineContentBytes { get; set; } = 16 * 1024;

    public int MaxUploadChunkBytes { get; set; } = 16 * 1024;

    public int MaxUploadBytes { get; set; } = 256 * 1024;

    public int MaxPageContentBytes { get; set; } = 128 * 1024;

    public int MaxListItemsLimit { get; set; } = 200;

    public int UploadIdleTimeoutSeconds { get; set; } = 15 * 60;
}
