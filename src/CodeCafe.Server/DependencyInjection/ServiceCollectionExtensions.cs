using CodeCafe.Infrastructure.Persistence;
using CodeCafe.Server.Auth;
using CodeCafe.Server.Configuration;
using CodeCafe.Server.Infrastructure;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using System.Security.Cryptography.X509Certificates;

namespace CodeCafe.Server.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeServerHost(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers();
        services.AddSingleton<DatabaseMigrationRunner>();
        services.AddOptions<AuthorizationServerOptions>()
            .Bind(configuration.GetSection(AuthorizationServerOptions.SectionName))
            .PostConfigure(options => options.ApplyEnvironmentDefaults(environment))
            .Validate(options => Uri.TryCreate(options.Issuer, UriKind.Absolute, out _),
                "AuthorizationServer:Issuer must be an absolute URI.")
            .Validate(options => Uri.TryCreate(options.FrontendBaseUrl, UriKind.Absolute, out _),
                "AuthorizationServer:FrontendBaseUrl must be an absolute URI.")
            .Validate(options => options.PublicClients.All(client =>
                    !string.IsNullOrWhiteSpace(client.ClientId)
                    && client.RedirectUris.Length > 0
                    && client.RedirectUris.All(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))),
                "AuthorizationServer:PublicClients entries must have a client id and absolute redirect URIs.")
            .Validate(options => !environment.IsProduction() || HasProductionCertificates(options),
                "Production AuthorizationServer configuration requires signing and encryption certificates via path or base64 value.")
            .ValidateOnStart();

        services.AddOptions<McpServerOptions>()
            .Bind(configuration.GetSection(McpServerOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.EndpointPath)
                && options.EndpointPath.StartsWith("/", StringComparison.Ordinal),
                "Mcp:EndpointPath must start with '/'.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ProtectedResourceMetadataPath)
                && options.ProtectedResourceMetadataPath.StartsWith("/", StringComparison.Ordinal),
                "Mcp:ProtectedResourceMetadataPath must start with '/'.")
            .Validate(options => !options.Enabled
                || !options.RequireAuthorization
                || !string.IsNullOrWhiteSpace(options.RequiredAudience),
                "Mcp protected resource auth requires RequiredAudience when enabled.")
            .ValidateOnStart();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>()
                    .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                var authOptions = GetAuthorizationServerOptions(configuration, environment);
                var mcpOptions = configuration.GetSection(McpServerOptions.SectionName).Get<McpServerOptions>() ?? new McpServerOptions();

                options.SetIssuer(new Uri(authOptions.Issuer, UriKind.Absolute));
                options.SetAuthorizationEndpointUris("/connect/authorize");
                options.SetTokenEndpointUris("/connect/token");

                options.AllowAuthorizationCodeFlow();
                options.AllowRefreshTokenFlow();
                options.RequireProofKeyForCodeExchange();
                options.RegisterScopes("notes.read", "notes.write");
                options.RegisterAudiences(McpResourceIdentifiers.GetAudienceValues(mcpOptions, authOptions));
                options.RegisterResources(McpResourceIdentifiers.GetResourceValues(mcpOptions, authOptions));
                options.AddEventHandler(OpenIddictDiscoveryMetadataHandler.Descriptor);

                var aspNetCoreBuilder = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough();

                if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
                {
                    options.AddDevelopmentEncryptionCertificate();
                    options.AddDevelopmentSigningCertificate();
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();
                }
                else
                {
                    options.AddEncryptionCertificate(LoadCertificate(
                        authOptions.EncryptionCertificatePath,
                        authOptions.EncryptionCertificateBase64,
                        authOptions.EncryptionCertificatePassword));
                    options.AddSigningCertificate(LoadCertificate(
                        authOptions.SigningCertificatePath,
                        authOptions.SigningCertificateBase64,
                        authOptions.SigningCertificatePassword));
                }
            })
            .AddValidation(options =>
            {
                var mcpOptions = configuration.GetSection(McpServerOptions.SectionName).Get<McpServerOptions>() ?? new McpServerOptions();

                options.UseLocalServer();
                options.AddAudiences(McpResourceIdentifiers.GetAudienceValues(mcpOptions, GetAuthorizationServerOptions(configuration, environment)));
                options.UseAspNetCore();
            });

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<OpenIddictSeedHostedService>();
        }

        return services;
    }

    private static AuthorizationServerOptions GetAuthorizationServerOptions(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration
            .GetSection(AuthorizationServerOptions.SectionName)
            .Get<AuthorizationServerOptions>()
            ?? new AuthorizationServerOptions();

        options.ApplyEnvironmentDefaults(environment);
        return options;
    }

    private static bool HasProductionCertificates(AuthorizationServerOptions options)
    {
        return HasCertificate(options.SigningCertificatePath, options.SigningCertificateBase64)
            && HasCertificate(options.EncryptionCertificatePath, options.EncryptionCertificateBase64);
    }

    private static bool HasCertificate(string path, string base64Value)
    {
        return !string.IsNullOrWhiteSpace(path) || !string.IsNullOrWhiteSpace(base64Value);
    }

    private static X509Certificate2 LoadCertificate(string path, string base64Value, string password)
    {
        if (!string.IsNullOrWhiteSpace(base64Value))
        {
            return X509CertificateLoader.LoadPkcs12(
                Convert.FromBase64String(base64Value),
                password,
                X509KeyStorageFlags.EphemeralKeySet);
        }

        return X509CertificateLoader.LoadPkcs12FromFile(
            path,
            password,
            X509KeyStorageFlags.EphemeralKeySet);
    }
}
