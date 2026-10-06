using CodeCafe.Application.Auth.ChangePassword;
using CodeCafe.Application.Auth.GetCurrentUser;
using CodeCafe.Application.Auth.Login;
using CodeCafe.Application.Auth.Logout;
using CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;
using CodeCafe.Application.Auth.PersonalAccessTokens.ListPersonalAccessTokens;
using CodeCafe.Application.Auth.PersonalAccessTokens.RevokePersonalAccessToken;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Auth.RefreshToken;
using CodeCafe.Application.Auth.Register;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Auth.UpdateProfile;
using CodeCafe.Application.Common;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    // Anonymous for the same reason as Refresh: the refresh token is the credential.
    [HttpPost("auth/logout")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiterExtensions.AuthPolicy)]
    public Task<Result> Logout(LogoutRequest request, CancellationToken cancellationToken)
        => sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);

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

    // The raw token is returned in this response only; afterwards the API stores just its hash.
    [HttpPost("auth/tokens")]
    [Authorize]
    public Task<Result<CreatedPersonalAccessTokenDto>> CreatePersonalAccessToken(CreatePersonalAccessTokenRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreatePersonalAccessTokenCommand(request.Name, request.ExpiresInDays), cancellationToken);

    [HttpGet("auth/tokens")]
    [Authorize]
    public Task<Result<IReadOnlyList<PersonalAccessTokenDto>>> ListPersonalAccessTokens(CancellationToken cancellationToken)
        => sender.Send(new ListPersonalAccessTokensQuery(), cancellationToken);

    [HttpDelete("auth/tokens/{id:guid}")]
    [Authorize]
    public Task<Result> RevokePersonalAccessToken(Guid id, CancellationToken cancellationToken)
        => sender.Send(new RevokePersonalAccessTokenCommand(id), cancellationToken);
}
