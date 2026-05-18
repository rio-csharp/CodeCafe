using CodeCafe.Infrastructure.Identity;
using CodeCafe.Infrastructure.Persistence;
using CodeCafe.WebApi.Auth;
using CodeCafe.WebApi.Configuration;
using CodeCafe.WebApi.Errors;
using CodeCafe.WebApi.Health;
using CodeCafe.WebApi.Infrastructure;
using CodeCafe.WebApi.Networking;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

        return services;
    }

    private static IServiceCollection AddCodeCafeDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName("CodeCafe")
            .PersistKeysToDbContext<ApplicationDbContext>();

        return services;
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
            options.Cookie.HttpOnly = false;
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
                var clientIp = clientIpAddressAccessor.GetClientIpAddress(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
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
                var clientIp = clientIpAddressAccessor.GetClientIpAddress(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
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
                var clientIp = clientIpAddressAccessor.GetClientIpAddress(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
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
