using CodeCafe.Api.Errors;
using Microsoft.AspNetCore.Antiforgery;

namespace CodeCafe.Api.Common;

public static class WebApplicationExtensions
{
    public static WebApplication UseCodeCafeApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseCodeCafeApiAntiforgery();
        app.UseAuthorization();
        app.MapCodeCafeApi();
        return app;
    }

    private static IApplicationBuilder UseCodeCafeApiAntiforgery(this IApplicationBuilder app)
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
}
