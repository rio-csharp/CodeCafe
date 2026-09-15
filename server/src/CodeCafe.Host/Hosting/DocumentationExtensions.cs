using Scalar.AspNetCore;

namespace CodeCafe.Host.Hosting;

internal static class DocumentationExtensions
{
    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        // The OpenAPI document describes every contract operation; exposing it in production
        // would hand attackers a map of the API surface.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();
        }

        return app;
    }
}
