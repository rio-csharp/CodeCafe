using CodeCafe.Application.Common.Configuration;
using CodeCafe.Application.Common.Uploads;
using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Infrastructure.Uploads;

namespace CodeCafe.Host.Mcp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeMcp(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpContextAccessor();
        services
            .AddOptions<McpOptions>()
            .Bind(configuration.GetSection(McpOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.EndpointPath)
                    && options.EndpointPath.StartsWith("/", StringComparison.Ordinal),
                "Mcp:EndpointPath must start with '/'."
            )
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.ProtectedResourceMetadataPath)
                    && options.ProtectedResourceMetadataPath.StartsWith(
                        "/",
                        StringComparison.Ordinal
                    ),
                "Mcp:ProtectedResourceMetadataPath must start with '/'."
            )
            .Validate(
                options =>
                    options.AllowedOrigins.All(origin =>
                        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    ),
                "Mcp:AllowedOrigins values must be absolute HTTP or HTTPS origins."
            )
            .Validate(
                options => options.MaxInlineContentBytes > 0,
                "Mcp:MaxInlineContentBytes must be greater than zero."
            )
            .Validate(
                options => options.MaxUploadChunkBytes > 0,
                "Mcp:MaxUploadChunkBytes must be greater than zero."
            )
            .Validate(
                options => options.MaxUploadBytes >= options.MaxUploadChunkBytes,
                "Mcp:MaxUploadBytes must be greater than or equal to MaxUploadChunkBytes."
            )
            .Validate(
                options => options.MaxPageContentBytes >= options.MaxInlineContentBytes,
                "Mcp:MaxPageContentBytes must be greater than or equal to MaxInlineContentBytes."
            )
            .Validate(
                options => options.MaxListItemsLimit > 0,
                "Mcp:MaxListItemsLimit must be greater than zero."
            )
            .Validate(
                options => options.UploadIdleTimeoutSeconds > 0,
                "Mcp:UploadIdleTimeoutSeconds must be greater than zero."
            )
            .Validate(
                options =>
                    !options.Enabled
                    || !options.RequireAuthorization
                    || !string.IsNullOrWhiteSpace(options.RequiredAudience),
                "Mcp protected resource auth requires RequiredAudience when enabled."
            )
            .ValidateOnStart();

        services
            .AddMcpServer()
            .WithHttpTransport(transportOptions =>
            {
                transportOptions.Stateless = true;
            })
            .WithTools<DiagnosticsMcpTools>()
            .WithTools<NotesReadMcpTools>()
            .WithTools<NotesMcpNotebookTools>()
            .WithTools<NotesMcpItemTools>()
            .WithResources<NotesMcpResources>()
            .WithPrompts<NotesMcpPrompts>();

        // Register shared upload infrastructure
        services.AddScoped<IUploadStore, DatabaseUploadStore>();
        services.AddScoped<IContentImporter, MarkdownContentImporter>();

        // MCP-specific services
        services.AddSingleton<IMcpMarkdownImporter, MarkdigMcpMarkdownImporter>();
        services.AddScoped<IMcpMutationExecutor, McpMutationExecutor>();

        return services;
    }
}
