using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CodeCafe.Host.Hosting;

// Declares the Bearer scheme so Scalar shows an Authorize button and generated clients know
// to send the access token. Declared unconditionally: the app authenticates with a custom
// handler (CodeCafeAuthenticationHandler), so there is no JwtBearer scheme to discover.
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT",
        };
        return Task.CompletedTask;
    }
}
