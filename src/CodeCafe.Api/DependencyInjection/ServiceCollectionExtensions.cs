using CodeCafe.Api.Configuration;
using CodeCafe.Api.Endpoints.Auth;
using CodeCafe.Api.Errors;
using CodeCafe.Api.Networking;
using CodeCafe.Infrastructure.Identity;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace CodeCafe.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeApi(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddSingleton<IClientIpAddressAccessor, ClientIpAddressAccessor>();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
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

        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .ValidateOnStart();

        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("CodeCafe");

        if (!environment.IsEnvironment("Testing"))
        {
            dataProtectionBuilder.PersistKeysToDbContext<ApplicationDbContext>();
        }

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        services.AddAuthorization();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var clientIpAddressAccessor = context.HttpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("CodeCafe.Api.RateLimiting");

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
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: true),
                    _ => new FixedWindowRateLimiterOptions
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
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false),
                    _ => new FixedWindowRateLimiterOptions
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
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy("oauth-registration", httpContext =>
            {
                var clientIpAddressAccessor = httpContext.RequestServices.GetRequiredService<IClientIpAddressAccessor>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(httpContext, clientIpAddressAccessor, allowAuthenticatedUserKey: false),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

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
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddScoped<IAuthEndpointService, IdentityAuthEndpointService>();
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
}
