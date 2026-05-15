using CodeCafe.Api.Authentication;
using CodeCafe.Api.Configuration;
using CodeCafe.Application.Auth;
using CodeCafe.Contracts.Auth;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Api.Controllers;


[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public sealed class AuthController(
    IAuthService authService,
    IAuthCookieManager authCookieManager) : ControllerBase
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
        var signInResult = authService.SignIn(request.Username, request.Password);

        if (signInResult is null)
        {
            return Unauthorized();
        }

        authCookieManager.Append(Response, Request.IsHttps, signInResult.Token);

        return Ok(new LoginSessionResponse(true, signInResult.Username));
    }

    [HttpPost("logout", Name = "Logout")]
    public IActionResult Logout()
    {
        authCookieManager.Delete(Response, Request.IsHttps);

        return NoContent();
    }
}
