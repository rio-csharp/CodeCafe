using CodeCafe.Api.Configuration;
using CodeCafe.Api.Endpoints.Auth;
using CodeCafe.Api.Errors;
using CodeCafe.Api.Networking;
using CodeCafe.Infrastructure.Identity;
using CodeCafe.Infrastructure.Persistence;
using CodeCafe.Mcp.Configuration;
using CodeCafe.Server.Auth;
using CodeCafe.Server.Configuration;
using CodeCafe.Server.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;

namespace CodeCafe.Server.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeServerHost(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddCors();
        services.AddCodeCafeCorsOptions(configuration, environment);
        services.AddCodeCafeForwardedHeaders();
        services.AddCodeCafeAntiforgery(environment);
        services.AddCodeCafeDataProtection(environment);
        services.AddCodeCafeAuthentication();
        services.AddCodeCafeRateLimiting();
        services.AddCodeCafeIdentity();
        services.AddScoped<IAuthSessionService, IdentityAuthSessionService>();
        services.AddCodeCafeApplicationCookie();
        services.AddCodeCafeShutdownOptions(configuration);
        services.AddCodeCafeHealthChecks(environment);
        services.AddSingleton<ServerDrainState>();
        services.AddHostedService<ServerDrainHostedService>();
        services.AddSingleton<IClientIpAddressAccessor, ClientIpAddressAccessor>();
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
            .Validate(options => options.PublicClients.All(client =>
                    OpenIddictClientRegistration.AreAllowedScopesValid(client.AllowedScopes, GetMcpOptions(configuration))),
                "AuthorizationServer:PublicClients entries must allow at least one configured MCP read/write scope and cannot include unsupported scopes.")
            .Validate(options => !environment.IsProduction() || HasProductionCertificates(options),
                "Production AuthorizationServer configuration requires signing and encryption certificates via path or base64 value.")
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
                var mcpOptions = configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();

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
                var mcpOptions = configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();

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

    private static IServiceCollection AddCodeCafeShutdownOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ShutdownOptions>()
            .Bind(configuration.GetSection(ShutdownOptions.SectionName))
            .Validate(options => options.TimeoutSeconds > 0,
                "Shutdown:TimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<HostOptions>()
            .Configure<IOptions<ShutdownOptions>>((options, shutdownOptionsAccessor) =>
            {
                options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownOptionsAccessor.Value.TimeoutSeconds);
            });

        return services;
    }

    private static IServiceCollection AddCodeCafeHealthChecks(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var healthChecks = services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("The host process is running."),
                tags: ["live", "ready"]);

        healthChecks.AddCheck<ServerDrainReadinessHealthCheck>(
            "drain",
            tags: ["ready"]);

        if (!environment.IsEnvironment("Testing"))
        {
            healthChecks.AddCheck<DatabaseReadinessHealthCheck>(
                "database",
                tags: ["ready"]);
        }

        return services;
    }

    private static IServiceCollection AddCodeCafeCorsOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .PostConfigure(options =>
            {
                if (environment.IsDevelopment() && options.AllowedOrigins.Length == 0)
                {
                    options.AllowedOrigins = CorsOptions.DevelopmentAllowedOrigins;
                }
            })
            .Validate(options => environment.IsDevelopment() || environment.IsEnvironment("Testing") || options.AllowedOrigins.Length > 0,
                "Cors:AllowedOrigins must be set in non-development environments.")
            .Validate(options => options.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)),
                "Cors:AllowedOrigins values must be absolute HTTP or HTTPS origins.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddCodeCafeForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 2;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var network in TrustedProxyNetworks.All)
            {
                options.KnownIPNetworks.Add(network);
            }
        });

        return services;
    }

    private static IServiceCollection AddCodeCafeAntiforgery(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "CodeCafe.Api.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.Path = "/";
        });

        return services;
    }

    private static IServiceCollection AddCodeCafeDataProtection(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("CodeCafe");

        if (!environment.IsEnvironment("Testing"))
        {
            dataProtectionBuilder.PersistKeysToDbContext<ApplicationDbContext>();
        }

        return services;
    }

    private static IServiceCollection AddCodeCafeAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        services.AddAuthorization();

        return services;
    }

    private static IServiceCollection AddCodeCafeRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRateLimitRejectedAsync;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: true),
                    _ => CreateFixedWindowRateLimiterOptions(300, TimeSpan.FromMinutes(1)));
            });

            options.AddPolicy("registration", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false),
                    _ => CreateFixedWindowRateLimiterOptions(3, TimeSpan.FromHours(1)));
            });

            options.AddPolicy("login", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false),
                    _ => CreateFixedWindowRateLimiterOptions(10, TimeSpan.FromMinutes(1)));
            });

            options.AddPolicy("oauth-registration", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false),
                    _ => CreateFixedWindowRateLimiterOptions(20, TimeSpan.FromHours(1)));
            });

            options.AddPolicy("mcp", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: true),
                    _ => CreateFixedWindowRateLimiterOptions(120, TimeSpan.FromMinutes(1)));
            });

            options.AddPolicy("ai", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: true),
                    _ => CreateFixedWindowRateLimiterOptions(30, TimeSpan.FromMinutes(1)));
            });
        });

        return services;
    }

    private static IServiceCollection AddCodeCafeIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddCodeCafeApplicationCookie(this IServiceCollection services)
    {
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "CodeCafe.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Problem(ApiProblems.Create(
                    "authentication_required",
                    "Authentication is required to access this resource.",
                    StatusCodes.Status401Unauthorized)).ExecuteAsync(context.HttpContext);
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Problem(ApiProblems.Create(
                    "access_denied",
                    "You do not have permission to access this resource.",
                    StatusCodes.Status403Forbidden)).ExecuteAsync(context.HttpContext);
            };
        });

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

    private static McpOptions GetMcpOptions(IConfiguration configuration)
    {
        return configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();
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

    private static ValueTask OnRateLimitRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var clientIpAddressAccessor = context.HttpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("CodeCafe.Server.RateLimiting");

        logger.LogWarning(
            "Rate limit rejected request. Path={Path}; ClientIp={ClientIp}",
            context.HttpContext.Request.Path,
            clientIpAddressAccessor.GetClientIpAddress(context.HttpContext));

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return new ValueTask(Results.Problem(ApiProblems.Create(
            "rate_limited",
            "Too many requests. Please try again later.",
            StatusCodes.Status429TooManyRequests)).ExecuteAsync(context.HttpContext));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowRateLimiterOptions(int permitLimit, TimeSpan window)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }

    private static string GetRateLimitPartitionKey(
        HttpContext httpContext,
        IClientIpAddressAccessor clientIpAddressAccessor,
        bool allowAuthenticatedUserKey)
    {
        if (allowAuthenticatedUserKey && httpContext.User.Identity?.IsAuthenticated == true)
        {
            var subject = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub");
            if (!string.IsNullOrWhiteSpace(subject))
            {
                return $"user:{subject}";
            }
        }

        return $"ip:{clientIpAddressAccessor.GetClientIpAddress(httpContext)}";
    }
}
