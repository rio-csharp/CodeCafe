using CodeCafe.Application.Auth.Commands;
using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Auth.Queries;
using CodeCafe.Application.Common;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("auth/register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicy)]
    public Task<Result<AuthSessionDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
        => sender.Send(new RegisterCommand(request.Email, request.Password, request.DisplayName), cancellationToken);

    [HttpPost("auth/login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicy)]
    public Task<Result<AuthSessionDto>> Login(LoginRequest request, CancellationToken cancellationToken)
        => sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

    // Exchanges a refresh token for a fresh session; anonymous because the refresh
    // token itself is the credential.
    [HttpPost("auth/refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicy)]
    public Task<Result<AuthSessionDto>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
        => sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

    [HttpPost("auth/logout")]
    [Authorize]
    public Task<Result> Logout(CancellationToken cancellationToken)
        => sender.Send(new LogoutCommand(), cancellationToken);

    [HttpGet("auth/me")]
    [Authorize]
    public Task<Result<AuthUserDto>> Me(CancellationToken cancellationToken)
        => sender.Send(new GetCurrentUserQuery(), cancellationToken);

    [HttpPatch("auth/me")]
    [Authorize]
    public Task<Result<AuthUserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
        => sender.Send(new UpdateProfileCommand(request.DisplayName), cancellationToken);

    [HttpPost("auth/change-password")]
    [Authorize]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicy)]
    public Task<Result> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
        => sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
}
