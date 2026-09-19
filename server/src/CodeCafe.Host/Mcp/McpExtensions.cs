using CodeCafe.Host.Hosting;
using CodeCafe.Host.Mcp.Resources;
using CodeCafe.Host.Mcp.Tools;
using Microsoft.Extensions.Options;

namespace CodeCafe.Host.Mcp;

internal static class McpExtensions
{
    public static IServiceCollection AddCodeCafeMcp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<McpOptions>()
            .Bind(configuration.GetSection(McpOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.EndpointPath)
                    && options.EndpointPath.StartsWith('/'),
                "Mcp:EndpointPath must start with '/'.")
            .ValidateOnStart();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools<NotebooksMcpTools>()
            .WithTools<PagesMcpTools>()
            .WithTools<BlocksMcpTools>()
            .WithTools<RevisionsMcpTools>()
            .WithTools<SearchMcpTools>()
            .WithTools<TrashMcpTools>()
            .WithTools<AiMcpTools>()
            .WithResources<NotebookMcpResources>();

        return services;
    }

    public static WebApplication MapCodeCafeMcp(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<McpOptions>>().Value;
        if (!options.Enabled)
            return app;

        // Bearer only (fallback policy); clients get a token via POST /api/auth/login first.
        app.MapMcp(options.EndpointPath).RequireRateLimiting(RateLimiterExtensions.McpPolicy);
        return app;
    }
}
