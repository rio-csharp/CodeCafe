using CodeCafe.Infrastructure.Identity;
using CodeCafe.WebApi.Errors;
using CodeCafe.WebApi.Networking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CodeCafe.WebApi.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IClientIpAddressAccessor clientIpAddressAccessor,
    IOptions<AuthOptions> authOptions,
    ILogger<AuthController> logger)
    : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("registration")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var clientIp = clientIpAddressAccessor.GetClientIpAddress(HttpContext);

        if (!authOptions.Value.RegistrationEnabled)
        {
            logger.LogInformation("Registration rejected because it is disabled. ClientIp={ClientIp}", clientIp);
            return ProblemFactory.Result(
                StatusCodes.Status403Forbidden,
                "registration_disabled",
                "Registration is currently disabled.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();

        if (displayName.Length == 0)
        {
            return ProblemFactory.Result(
                StatusCodes.Status400BadRequest,
                "invalid_display_name",
                "Display name is required.");
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogInformation("Registration rejected for existing email. ClientIp={ClientIp}", clientIp);
            return ProblemFactory.Result(
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

            return ProblemFactory.Result(
                StatusCodes.Status400BadRequest,
                "registration_failed",
                "Registration failed. Please check the submitted values.");
        }

        await signInManager.SignInAsync(user, isPersistent: true);

        logger.LogInformation("User registered. UserId={UserId}; ClientIp={ClientIp}", user.Id, clientIp);

        return Ok(new AuthResponse(ToResponse(user)));
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var clientIp = clientIpAddressAccessor.GetClientIpAddress(HttpContext);
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

        return Ok(new AuthResponse(ToResponse(user)));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType<LogoutResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LogoutResponse>> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new LogoutResponse(true));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);

        return user is null
            ? Unauthorized()
            : Ok(new AuthResponse(ToResponse(user)));
    }

    private ObjectResult InvalidCredentials()
    {
        return ProblemFactory.Result(
            StatusCodes.Status401Unauthorized,
            "invalid_credentials",
            "Invalid email or password.");
    }

    private static UserResponse ToResponse(ApplicationUser user)
    {
        return new UserResponse(user.Id, user.Email ?? string.Empty, user.DisplayName);
    }
}
