using CodeCafe.Api.Configuration;
using CodeCafe.Api.Networking;
using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Commands.AuthenticateUser;
using CodeCafe.Application.Auth.Commands.RegisterUser;
using CodeCafe.Application.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

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
        [FromServices] ISender sender,
        [FromServices] IAuthSessionService authSessionService,
        [FromServices] IClientIpAddressAccessor clientIpAddressAccessor,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] IOptions<AuthOptions> authOptions,
        RegisterRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var clientIp = clientIpAddressAccessor.GetClientIpAddress(httpContext);
        var logger = loggerFactory.CreateLogger("CodeCafe.Api.Auth");
        var result = await sender.Send(
            new RegisterUserCommand(
                authOptions.Value.RegistrationEnabled,
                request.Email,
                request.Password,
                request.DisplayName),
            cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogInformation(
                "Registration rejected. Code={Code}; ClientIp={ClientIp}",
                result.Error!.Code,
                clientIp);
            return ToResult(result);
        }

        await authSessionService.SignInAsync(result.Value!.Id, isPersistent: true);
        logger.LogInformation(
            "User registered. UserId={UserId}; ClientIp={ClientIp}",
            result.Value.Id,
            clientIp);

        return ToResult(result);
    }

    private static async Task<IResult> LoginAsync(
        [FromServices] ISender sender,
        [FromServices] IAuthSessionService authSessionService,
        [FromServices] IClientIpAddressAccessor clientIpAddressAccessor,
        [FromServices] ILoggerFactory loggerFactory,
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var clientIp = clientIpAddressAccessor.GetClientIpAddress(httpContext);
        var logger = loggerFactory.CreateLogger("CodeCafe.Api.Auth");
        var result = await sender.Send(
            new AuthenticateUserCommand(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogInformation(
                "Login failed. Code={Code}; ClientIp={ClientIp}",
                result.Error!.Code,
                clientIp);
            return ToResult(result);
        }

        await authSessionService.SignInAsync(result.Value!.Id, isPersistent: true);
        logger.LogInformation(
            "User logged in. UserId={UserId}; ClientIp={ClientIp}",
            result.Value.Id,
            clientIp);

        return ToResult(result);
    }

    private static async Task<IResult> LogoutAsync([FromServices] IAuthSessionService authSessionService)
    {
        await authSessionService.SignOutAsync();
        return TypedResults.Ok(new LogoutResponse(true));
    }

    private static async Task<IResult> MeAsync(
        [FromServices] ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(httpContext.User);
        var result = await sender.Send(new GetCurrentUserQuery(currentUserId), cancellationToken);
        return ToResult(result);
    }

    private static IResult ToResult(AuthResult<AuthUserModel> result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new AuthResponse(ToUserResponse(result.Value!)));
        }

        return TypedResults.Problem(
            detail: result.Error!.Message,
            statusCode: ToStatusCode(result.Error.Kind),
            title: result.Error.Code);
    }

    private static int ToStatusCode(AuthFailureKind kind)
    {
        return kind switch
        {
            AuthFailureKind.Validation => StatusCodes.Status400BadRequest,
            AuthFailureKind.Unauthorized => StatusCodes.Status401Unauthorized,
            AuthFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            AuthFailureKind.Conflict => StatusCodes.Status409Conflict,
            AuthFailureKind.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static UserResponse ToUserResponse(AuthUserModel user)
    {
        return new UserResponse(user.Id, user.Email, user.DisplayName);
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : Guid.Empty;
    }
}
