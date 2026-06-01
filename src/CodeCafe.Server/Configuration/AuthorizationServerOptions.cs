using Microsoft.Extensions.Hosting;

namespace CodeCafe.Server.Configuration;

public sealed class AuthorizationServerOptions
{
    public const string SectionName = "AuthorizationServer";
    public const string DevelopmentIssuer = "https://localhost:7239/";
    public const string TestingIssuer = "https://codecafe.test/";
    public const string DevelopmentFrontendBaseUrl = "http://localhost:5173";

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
                "http://localhost/callback",
                "http://127.0.0.1/callback"
            ]
        }
    ];

    public string SigningCertificatePath { get; set; } = string.Empty;

    public string SigningCertificateBase64 { get; set; } = string.Empty;

    public string SigningCertificatePassword { get; set; } = string.Empty;

    public string EncryptionCertificatePath { get; set; } = string.Empty;

    public string EncryptionCertificateBase64 { get; set; } = string.Empty;

    public string EncryptionCertificatePassword { get; set; } = string.Empty;

    public void ApplyEnvironmentDefaults(IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(FrontendBaseUrl)
            && (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            FrontendBaseUrl = DevelopmentFrontendBaseUrl;
        }

        if (!string.IsNullOrWhiteSpace(Issuer))
        {
            return;
        }

        if (environment.IsDevelopment())
        {
            Issuer = DevelopmentIssuer;
        }
        else if (environment.IsEnvironment("Testing"))
        {
            Issuer = TestingIssuer;
        }
    }
}

public sealed class OAuthClientOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string[] RedirectUris { get; set; } = [];
}
