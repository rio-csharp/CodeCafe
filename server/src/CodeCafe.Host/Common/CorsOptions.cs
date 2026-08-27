namespace CodeCafe.Host.Common;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public static readonly string[] DevelopmentAllowedOrigins =
    [
        "http://localhost:5173",
        "https://localhost:5173",
        "http://127.0.0.1:5173",
        "https://127.0.0.1:5173",
    ];

    public string[] AllowedOrigins { get; set; } = [];
}
