namespace CodeCafe.Api.Endpoints;

using CodeCafe.Contracts.System;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/system")
            .WithTags("System");

        group.MapGet("/info", (IHostEnvironment environment) => Results.Ok(new SystemInfoResponse(
            "CodeCafe",
            environment.EnvironmentName,
            DateTimeOffset.UtcNow)))
        .WithName("GetSystemInfo");

        return app;
    }
}
