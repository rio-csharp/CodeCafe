using CodeCafe.WebApi.Health;
using Serilog;

namespace CodeCafe.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseCodeCafePipeline(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseSerilogRequestLogging();
        app.UseForwardedHeaders();

        if (!app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }

        app.UseCodeCafeCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseCodeCafeAntiforgery();
        app.UseAuthorization();

        app.MapHealthEndpoints();
        app.MapCodeCafeCsrfEndpoint();
        app.MapControllers();

        return app;
    }

    private static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "Live" }));
        app.MapGet("/health/ready", (ReadinessState readinessState) =>
        {
            return readinessState.IsReady
                ? Results.Ok(new { status = "Ready" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });
    }
}
