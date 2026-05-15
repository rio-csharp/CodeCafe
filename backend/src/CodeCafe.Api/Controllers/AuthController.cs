using CodeCafe.Api.Authentication;
using CodeCafe.Api.Configuration;
using CodeCafe.Contracts.Auth;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Api.Controllers;


[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public sealed class AuthController(IAuthSessionService authSessionService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("session", Name = "GetSession")]
    public ActionResult<LoginSessionResponse> GetSession()
    {
        return Ok(new LoginSessionResponse(User.Identity?.IsAuthenticated == true, User.Identity?.Name));
    }

    [AllowAnonymous]
    [EnableRateLimiting(ApiPolicyNames.LoginRateLimit)]
    [HttpPost("login", Name = "Login")]
    public ActionResult<LoginSessionResponse> Login(LoginRequest request)
    {
        var username = authSessionService.SignIn(HttpContext, request.Username, request.Password);

        if (username is null)
        {
            return Unauthorized();
        }

        return Ok(new LoginSessionResponse(true, username));
    }

    [HttpPost("logout", Name = "Logout")]
    public IActionResult Logout()
    {
        authSessionService.SignOut(HttpContext);

        return NoContent();
    }
}
