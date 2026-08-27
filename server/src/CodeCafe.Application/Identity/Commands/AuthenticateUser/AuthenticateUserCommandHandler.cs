using CodeCafe.Application.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace CodeCafe.Application.Identity.Commands.AuthenticateUser;

public sealed class AuthenticateUserCommandHandler(
    IAuthUserGateway authUserGateway,
    ILogger<AuthenticateUserCommandHandler> logger
) : ICommandHandler<AuthenticateUserCommand, AuthResult<AuthUserModel>>
{
    public async Task<AuthResult<AuthUserModel>> Handle(
        AuthenticateUserCommand request,
        CancellationToken cancellationToken
    )
    {
        var email = AuthInput.NormalizeEmail(request.Email);
        var verificationResult = await authUserGateway.VerifyPasswordAsync(
            email,
            request.Password,
            lockoutOnFailure: true,
            cancellationToken
        );

        if (verificationResult.Succeeded)
        {
            return AuthResult<AuthUserModel>.Success(verificationResult.User!);
        }

        if (verificationResult.IsLockedOut)
        {
            // Lockout is a security-relevant event, so it is logged with the
            // client IP; the outward response stays a uniform
            // invalid_credentials failure to avoid account enumeration.
            logger.LogWarning(
                "Login denied for locked-out account. ClientIp={ClientIp}",
                request.ClientIp
            );
        }

        return InvalidCredentials();
    }

    private static AuthResult<AuthUserModel> InvalidCredentials()
    {
        return AuthResult<AuthUserModel>.Failure(
            AuthFailureKind.Unauthorized,
            "invalid_credentials",
            "Invalid email or password."
        );
    }
}
