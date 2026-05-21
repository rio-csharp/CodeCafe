namespace CodeCafe.WebApi.Auth;

public sealed class AuthorizationServerOptions
{
    public const string SectionName = "AuthorizationServer";

    public string Issuer { get; set; } = string.Empty;

    public string FrontendBaseUrl { get; set; } = string.Empty;

    public OAuthClientOptions[] PublicClients { get; set; } =
    [
        new()
        {
            ClientId = "codecafe-claude",
            DisplayName = "Claude Code",
            RedirectUris =
            [
                "http://localhost/",
                "http://127.0.0.1/"
            ]
        }
    ];

    public string SigningCertificatePath { get; set; } = string.Empty;

    public string SigningCertificateBase64 { get; set; } = string.Empty;

    public string SigningCertificatePassword { get; set; } = string.Empty;

    public string EncryptionCertificatePath { get; set; } = string.Empty;

    public string EncryptionCertificateBase64 { get; set; } = string.Empty;

    public string EncryptionCertificatePassword { get; set; } = string.Empty;
}

public sealed class OAuthClientOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string[] RedirectUris { get; set; } = [];
}
