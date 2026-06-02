using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapGet("/csrf", GetCsrfAsync)
            .AllowAnonymous();
        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting("registration");
        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting("login");
        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();
        group.MapGet("/me", MeAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static IResult GetCsrfAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        return TypedResults.Ok(new
        {
            token = tokens.RequestToken
        });
    }

    private static async Task<IResult> RegisterAsync(
        [FromServices] IAuthEndpointService authEndpointService,
        RegisterRequest request,
        HttpContext httpContext)
    {
        var result = await authEndpointService.RegisterAsync(request, httpContext);
        return ToResult(result);
    }

    private static async Task<IResult> LoginAsync(
        [FromServices] IAuthEndpointService authEndpointService,
        LoginRequest request,
        HttpContext httpContext)
    {
        var result = await authEndpointService.LoginAsync(request, httpContext);
        return ToResult(result);
    }

    private static async Task<IResult> LogoutAsync([FromServices] IAuthEndpointService authEndpointService)
    {
        var result = await authEndpointService.LogoutAsync();
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> MeAsync(
        [FromServices] IAuthEndpointService authEndpointService,
        HttpContext httpContext)
    {
        var result = await authEndpointService.MeAsync(httpContext.User);
        return ToResult(result);
    }

    private static IResult ToResult<T>(AuthOperationResult<T> result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Json(result.Value, statusCode: result.StatusCode);
        }

        return TypedResults.Problem(
            detail: result.Message,
            statusCode: result.StatusCode,
            title: result.Code);
    }
}
