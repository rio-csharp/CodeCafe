using CodeCafe.Application.Identity;
using CodeCafe.Application.Identity.Commands.AuthenticateUser;
using CodeCafe.Application.Identity.Commands.RegisterUser;
using CodeCafe.Application.Identity.Queries.GetCurrentUser;
using CodeCafe.Modules.Identity.Presentation.Configuration;
using CodeCafe.Modules.Identity.Presentation.Networking;
using CodeCafe.Shared.Presentation.Errors;
using CodeCafe.Application.Common.Identity;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeCafe.Modules.Identity.Presentation.Endpoints.Auth;

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
            new AuthenticateUserCommand(request.Email, request.Password, clientIp),
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
        [FromServices] ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        var result = await sender.Send(new GetCurrentUserQuery(currentUserId), cancellationToken);
        return ToResult(result);
    }

    private static IResult ToResult(AuthResult<AuthUserModel> result)
    {
        if (result.Succeeded)
        {
            return TypedResults.Ok(new AuthResponse(ToUserResponse(result.Value!)));
        }

        return TypedResults.Problem(ApiProblems.Create(
            result.Error!.Code,
            result.Error.Message,
            ToStatusCode(result.Error.Kind)));
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
}
