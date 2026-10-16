using System.Security.Claims;
using System.Threading.RateLimiting;
using CodeCafe.Application.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Hosting;

internal static class RateLimiterExtensions
{
    public const string AuthPolicy = "auth";
    public const string AiPolicy = "ai";
    public const string McpPolicy = "mcp";
    public const string AccessCodePolicy = "access-code";

    public static IServiceCollection AddCodeCafeRateLimiter(this IServiceCollection services)
        => services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;

            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

            options.AddPolicy(AiPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

            options.AddPolicy(McpPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));

            options.AddPolicy(AccessCodePolicy, context =>
                context.Request.Headers.ContainsKey(AccessCodeHeader.Name)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        PartitionKey(context),
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) })
                    : RateLimitPartition.GetNoLimiter("access-code:absent"));
        });

    // Authenticated callers get their own per-user bucket; only anonymous traffic
    // falls back to IP, so one noisy user cannot exhaust someone else's quota.
    private static string PartitionKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(subject))
                return $"user:{subject}";
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("CodeCafe.Host.RateLimiting");
        logger.LogWarning(
            "Rate limit rejected request. Path={Path}; Partition={Partition}",
            context.HttpContext.Request.Path,
            PartitionKey(context.HttpContext));

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ErrorResponses.WriteAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            "rate_limited",
            "Too many requests.",
            ErrorKind.RateLimited,
            cancellationToken);
    }
}
