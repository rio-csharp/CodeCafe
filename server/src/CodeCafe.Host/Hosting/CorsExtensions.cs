using Microsoft.Extensions.Options;
using CorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace CodeCafe.Host.Hosting;

internal sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}

internal static class CorsExtensions
{
    public static IServiceCollection AddCodeCafeCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<CorsSettings>()
            .Bind(configuration.GetSection(CorsSettings.SectionName))
            .PostConfigure(options =>
            {
                if (environment.IsDevelopment() && options.AllowedOrigins.Length == 0)
                    options.AllowedOrigins = ["http://localhost:5173"];
            })
            .Validate(
                options => environment.IsDevelopment() || options.AllowedOrigins.Length > 0,
                "Cors:AllowedOrigins must be set in non-development environments.")
            .Validate(
                options => options.AllowedOrigins.All(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    && (uri.Scheme is "http" or "https")),
                "Cors:AllowedOrigins values must be absolute HTTP or HTTPS origins.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSettings>>((corsOptions, settingsAccessor) =>
                corsOptions.AddDefaultPolicy(policy => policy
                    .WithOrigins(settingsAccessor.Value.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

        return services;
    }
}
