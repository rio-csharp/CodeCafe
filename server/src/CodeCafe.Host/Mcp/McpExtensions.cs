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

        services.AddMcpServer(options =>
            // Sent with every initialize, so the rule is in context before the first import
            // rather than buried in one tool description the model may never read.
            options.ServerInstructions =
                "CodeCafe keeps notebooks, pages, and blocks. Importing markdown through a tool "
                + "argument makes the model read and re-emit every byte, so for anything larger than a "
                + "few pages call codecafe_prepare_page_import or codecafe_prepare_notebook_import "
                + "first. It returns a short-lived upload URL and a temporary token scoped to one "
                + "target; POST the file there, then report the result. Never print the token and never "
                + "pass the file contents through a tool argument."
        )
            // Sessions live in process memory, so in the default stateful mode every request after
            // initialize has to reach the pod that created the session; behind more than one replica
            // without session affinity the rest come back as 404 "Session not found" and clients
            // abort the connection. Nothing here keeps per-session state — each request carries its
            // own bearer token and every tool is a static MediatR call — so stateless mode removes
            // the affinity requirement instead of buying sticky routing that MCP clients cannot honour.
            .WithHttpTransport(options => options.Stateless = true)
            .WithTools<NotebooksMcpTools>()
            .WithTools<PagesMcpTools>()
            .WithTools<BlocksMcpTools>()
            .WithTools<RevisionsMcpTools>()
            .WithTools<SearchMcpTools>()
            .WithTools<TrashMcpTools>()
            .WithTools<ImportMcpTools>()
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
