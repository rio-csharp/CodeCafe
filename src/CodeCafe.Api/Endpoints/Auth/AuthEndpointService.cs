using System.Security.Claims;
using CodeCafe.Api.Configuration;
using CodeCafe.Api.Networking;
using CodeCafe.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CodeCafe.Api.Endpoints.Auth;

public interface IAuthEndpointService
{
    Task<AuthOperationResult<AuthResponse>> RegisterAsync(RegisterRequest request, HttpContext httpContext);

    Task<AuthOperationResult<AuthResponse>> LoginAsync(LoginRequest request, HttpContext httpContext);

    Task<AuthOperationResult<AuthResponse>> MeAsync(ClaimsPrincipal user);

    Task<LogoutResponse> LogoutAsync();
}

public sealed class IdentityAuthEndpointService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IClientIpAddressAccessor clientIpAddressAccessor,
    IOptions<AuthOptions> authOptions,
    ILogger<IdentityAuthEndpointService> logger) : IAuthEndpointService
{
    public async Task<AuthOperationResult<AuthResponse>> RegisterAsync(RegisterRequest request, HttpContext httpContext)
    {
        var clientIp = clientIpAddressAccessor.GetClientIpAddress(httpContext);

        if (!authOptions.Value.RegistrationEnabled)
        {
            logger.LogInformation("Registration rejected because it is disabled. ClientIp={ClientIp}", clientIp);
            return AuthOperationResult<AuthResponse>.Failure(
                StatusCodes.Status403Forbidden,
                "registration_disabled",
                "Registration is currently disabled.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();

        if (displayName.Length == 0)
        {
            return AuthOperationResult<AuthResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "invalid_display_name",
                "Display name is required.");
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogInformation("Registration rejected for existing email. ClientIp={ClientIp}", clientIp);
            return AuthOperationResult<AuthResponse>.Failure(
                StatusCodes.Status409Conflict,
                "email_already_registered",
                "A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            logger.LogInformation(
                "Registration failed. ClientIp={ClientIp}; Errors={Errors}",
                clientIp,
                string.Join(", ", result.Errors.Select(error => error.Code)));

            return AuthOperationResult<AuthResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "registration_failed",
                "Registration failed. Please check the submitted values.");
        }

        await signInManager.SignInAsync(user, isPersistent: true);

        logger.LogInformation("User registered. UserId={UserId}; ClientIp={ClientIp}", user.Id, clientIp);

        return AuthOperationResult<AuthResponse>.Success(
            StatusCodes.Status200OK,
            new AuthResponse(ToUserResponse(user)));
    }

    public async Task<AuthOperationResult<AuthResponse>> LoginAsync(LoginRequest request, HttpContext httpContext)
    {
        var clientIp = clientIpAddressAccessor.GetClientIpAddress(httpContext);
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            logger.LogInformation("Login failed for unknown email. ClientIp={ClientIp}", clientIp);
            return InvalidCredentials();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            logger.LogInformation(
                "Login failed. UserId={UserId}; ClientIp={ClientIp}; IsLockedOut={IsLockedOut}",
                user.Id,
                clientIp,
                result.IsLockedOut);

            return InvalidCredentials();
        }

        await signInManager.SignInAsync(user, isPersistent: true);

        logger.LogInformation("User logged in. UserId={UserId}; ClientIp={ClientIp}", user.Id, clientIp);

        return AuthOperationResult<AuthResponse>.Success(
            StatusCodes.Status200OK,
            new AuthResponse(ToUserResponse(user)));
    }

    public async Task<AuthOperationResult<AuthResponse>> MeAsync(ClaimsPrincipal user)
    {
        var applicationUser = await userManager.GetUserAsync(user);
        return applicationUser is null
            ? AuthOperationResult<AuthResponse>.Failure(
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                "Authentication is required.")
            : AuthOperationResult<AuthResponse>.Success(
                StatusCodes.Status200OK,
                new AuthResponse(ToUserResponse(applicationUser)));
    }

    public async Task<LogoutResponse> LogoutAsync()
    {
        await signInManager.SignOutAsync();
        return new LogoutResponse(true);
    }

    private static AuthOperationResult<AuthResponse> InvalidCredentials()
    {
        return AuthOperationResult<AuthResponse>.Failure(
            StatusCodes.Status401Unauthorized,
            "invalid_credentials",
            "Invalid email or password.");
    }

    private static UserResponse ToUserResponse(ApplicationUser user)
    {
        return new UserResponse(user.Id, user.Email ?? string.Empty, user.DisplayName);
    }
}

public sealed record AuthOperationResult<T>(
    int StatusCode,
    T? Value,
    string? Code,
    string? Message)
{
    public bool Succeeded => Value is not null;

    public static AuthOperationResult<T> Success(int statusCode, T value) =>
        new(statusCode, value, null, null);

    public static AuthOperationResult<T> Failure(int statusCode, string code, string message) =>
        new(statusCode, default, code, message);
}
