using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCafe.Host.Hosting;

internal sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    // CIDR ranges whose X-Forwarded-For headers are trusted. Defaults to loopback so
    // local development works; deployments behind a reverse proxy must list its egress
    // networks here, otherwise client IPs (and IP-partitioned rate limits) collapse
    // into the proxy address, and broader ranges let clients spoof X-Forwarded-For.
    public string[] KnownNetworks { get; set; } = ["127.0.0.0/8", "::1/128"];

    // Single-proxy topology: exactly one forwarding hop is trusted.
    public int ForwardLimit { get; set; } = 1;
}

internal static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddCodeCafeForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ForwardedHeadersSettings>()
            .Bind(configuration.GetSection(ForwardedHeadersSettings.SectionName))
            .Validate(
                settings => settings.ForwardLimit > 0,
                "ForwardedHeaders:ForwardLimit must be greater than zero.")
            .Validate(
                settings => settings.KnownNetworks.All(network => System.Net.IPNetwork.TryParse(network, out _)),
                "ForwardedHeaders:KnownNetworks entries must be valid CIDR ranges (e.g. \"10.0.0.0/8\").")
            .ValidateOnStart();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ForwardedHeadersSettings>>((options, settingsAccessor) =>
            {
                var settings = settingsAccessor.Value;
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = settings.ForwardLimit;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                foreach (var network in settings.KnownNetworks)
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            });

        return services;
    }

    public static WebApplication UseCodeCafeForwardedHeaders(this WebApplication app)
    {
        app.UseForwardedHeaders();

        var settings = app.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value;
        if (app.Environment.IsProduction()
            && (settings.KnownNetworks.Length == 0 || settings.KnownNetworks.All(IsLoopbackNetwork)))
        {
            app.Logger.LogWarning(
                "ForwardedHeaders:KnownNetworks trusts no proxy network in Production. "
                + "Client IPs will resolve to the ingress address, collapsing IP-partitioned rate limits "
                + "into one shared partition. Configure the egress CIDR ranges of your reverse proxy.");
        }

        return app;
    }

    private static bool IsLoopbackNetwork(string network)
        => System.Net.IPNetwork.TryParse(network, out var parsed) && IPAddress.IsLoopback(parsed.BaseAddress);
}
