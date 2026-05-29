using CodeCafe.Infrastructure.Identity;
using CodeCafe.Infrastructure.Persistence;
using CodeCafe.WebApi.Auth;
using CodeCafe.WebApi.Configuration;
using CodeCafe.WebApi.Errors;
using CodeCafe.WebApi.Health;
using CodeCafe.WebApi.Infrastructure;
using CodeCafe.WebApi.Mcp;
using CodeCafe.WebApi.Networking;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;

namespace CodeCafe.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton<ReadinessState>();
        services.AddSingleton<DatabaseMigrationRunner>();
        services.AddHostedService<ReadinessShutdownService>();
        services.AddSingleton<IClientIpAddressAccessor, ClientIpAddressAccessor>();
        services.AddProblemDetails();
        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var error = new ApiError("invalid_request", "The request body is invalid.");
                return new BadRequestObjectResult(error);
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();

        services.AddCodeCafeOptions(configuration, environment);
        services.AddCodeCafeDataProtection();
        services.AddCodeCafeIdentity();
        services.AddCodeCafeCors();
        services.AddCodeCafeAntiforgery(environment);
        services.AddCodeCafeRateLimiting();
        services.AddCodeCafeCookieAuthentication(environment);
        services.AddCodeCafeOpenIddict(configuration, environment);
        services.AddCodeCafeMcp(configuration, environment);
        services.AddScoped<IMcpMutationExecutor, McpMutationExecutor>();

        return services;
    }

    private static IServiceCollection AddCodeCafeOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateOnStart();

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

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .PostConfigure(options =>
            {
                if (environment.IsDevelopment() && options.AllowedOrigins.Length == 0)
                {
                    options.AllowedOrigins = CorsOptions.DevelopmentAllowedOrigins;
                }
            })
            .Validate(options => environment.IsDevelopment() || options.AllowedOrigins.Length > 0,
                "Cors:AllowedOrigins must be set in non-development environments.")
            .Validate(options => options.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)),
                "Cors:AllowedOrigins values must be absolute HTTP or HTTPS origins.")
            .ValidateOnStart();

        services.AddOptions<McpOptions>()
            .Bind(configuration.GetSection(McpOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.EndpointPath)
                && options.EndpointPath.StartsWith("/", StringComparison.Ordinal),
                "Mcp:EndpointPath must start with '/'.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ProtectedResourceMetadataPath)
                && options.ProtectedResourceMetadataPath.StartsWith("/", StringComparison.Ordinal),
                "Mcp:ProtectedResourceMetadataPath must start with '/'.")
            .Validate(options => options.AllowedOrigins.All(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)),
                "Mcp:AllowedOrigins values must be absolute HTTP or HTTPS origins.")
            .Validate(options => !options.Enabled
                || !options.RequireAuthorization
                || !string.IsNullOrWhiteSpace(options.RequiredAudience),
                "Mcp protected resource auth requires RequiredAudience when enabled.")
            .Validate(options => !options.Enabled
                || !environment.IsProduction()
                || (options.RequireAuthorization
                    && !string.Equals(configuration["AllowedHosts"], "*", StringComparison.Ordinal)
                    && options.AllowedOrigins.Length > 0
                    && !string.IsNullOrWhiteSpace(options.RequiredAudience)),
                "Production MCP exposure requires authorization, explicit AllowedHosts, origins, and a configured audience/resource identifier.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddCodeCafeMcp(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            services.AddHttpContextAccessor();
        }

        services.AddMcpServer()
            .WithHttpTransport(transportOptions =>
            {
                transportOptions.Stateless = true;
            })
            .WithTools<NotesMcpNotebookTools>()
            .WithTools<NotesMcpItemTools>()
            .WithResources<NotesMcpResources>()
            .WithPrompts<NotesMcpPrompts>();

        return services;
    }

    private static IServiceCollection AddCodeCafeOpenIddict(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHostedService<OpenIddictSeedHostedService>();

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

                options.SetIssuer(new Uri(authOptions.Issuer, UriKind.Absolute));
                options.SetAuthorizationEndpointUris("/connect/authorize");
                options.SetTokenEndpointUris("/connect/token");

                options.AllowAuthorizationCodeFlow();
                options.AllowRefreshTokenFlow();
                options.RequireProofKeyForCodeExchange();
                options.RegisterScopes("notes.read", "notes.write");
                options.RegisterAudiences(GetMcpAudienceValues(configuration, environment));
                options.RegisterResources(GetMcpResourceValues(configuration, environment));
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
                var mcpOptions = configuration
                    .GetSection(McpOptions.SectionName)
                    .Get<McpOptions>()
                    ?? new McpOptions();

                options.UseLocalServer();
                options.AddAudiences(GetMcpAudienceValues(configuration, environment));
                options.UseAspNetCore();
            });

        return services;
    }

    private static IServiceCollection AddCodeCafeDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName("CodeCafe")
            .PersistKeysToDbContext<ApplicationDbContext>();

        return services;
    }

    private static bool HasProductionCertificates(AuthorizationServerOptions options)
    {
        return HasCertificate(options.SigningCertificatePath, options.SigningCertificateBase64)
            && HasCertificate(options.EncryptionCertificatePath, options.EncryptionCertificateBase64);
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

    private static bool HasCertificate(string path, string base64Value)
    {
        return !string.IsNullOrWhiteSpace(path) || !string.IsNullOrWhiteSpace(base64Value);
    }

    private static string[] GetMcpAudienceValues(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var mcpOptions = configuration
            .GetSection(McpOptions.SectionName)
            .Get<McpOptions>()
            ?? new McpOptions();
        var authOptions = GetAuthorizationServerOptions(configuration, environment);

        return McpResourceIdentifiers.GetAudienceValues(mcpOptions, authOptions);
    }

    private static string[] GetMcpResourceValues(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var mcpOptions = configuration
            .GetSection(McpOptions.SectionName)
            .Get<McpOptions>()
            ?? new McpOptions();
        var authOptions = GetAuthorizationServerOptions(configuration, environment);

        return McpResourceIdentifiers.GetResourceValues(mcpOptions, authOptions);
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

    private static IServiceCollection AddCodeCafeIdentity(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddAuthorization();

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

    private static IServiceCollection AddCodeCafeCors(this IServiceCollection services)
    {
        services.AddCors();
        return services;
    }

    private static IServiceCollection AddCodeCafeAntiforgery(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "CodeCafe.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.Path = "/";
        });

        return services;
    }

    private static IServiceCollection AddCodeCafeRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var clientIpAddressAccessor = context.HttpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("CodeCafe.RateLimiting");

                logger.LogWarning(
                    "Rate limit rejected request. Path={Path}; ClientIp={ClientIp}",
                    context.HttpContext.Request.Path,
                    clientIpAddressAccessor.GetClientIpAddress(context.HttpContext));

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ApiError("rate_limited", "Too many requests. Please try again later."),
                    cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                var partitionKey = GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: true);

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("registration", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                var partitionKey = GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false);

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("login", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                var partitionKey = GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false);

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("mcp", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                var partitionKey = GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: true);

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("oauth-registration", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                var partitionKey = GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false);

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
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

    private static IServiceCollection AddCodeCafeCookieAuthentication(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "CodeCafe.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        return services;
    }

    public static IApplicationBuilder UseCodeCafeCors(this WebApplication app)
    {
        var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;

        app.UseCors(policy =>
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });

        return app;
    }

    public static IEndpointRouteBuilder MapCodeCafeCsrfEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/csrf", (HttpContext httpContext, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return Results.Ok(new { token = tokens.RequestToken });
        })
        .AllowAnonymous();

        return app;
    }

    public static IApplicationBuilder UseCodeCafeAntiforgery(this IApplicationBuilder app)
    {
        return app.Use(async (httpContext, next) =>
        {
            if (RequiresCsrfValidation(httpContext.Request))
            {
                var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
                await antiforgery.ValidateRequestAsync(httpContext);
            }

            await next(httpContext);
        });
    }

    private static bool RequiresCsrfValidation(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method)
            || HttpMethods.IsDelete(request.Method);
    }

    public static IServiceCollection AddCodeCafeForwardedHeaders(this IServiceCollection services)
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
}
