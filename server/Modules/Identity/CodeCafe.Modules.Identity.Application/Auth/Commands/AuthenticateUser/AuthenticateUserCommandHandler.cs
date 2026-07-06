using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Auth.Commands.AuthenticateUser;

public sealed class AuthenticateUserCommandHandler(
    IAuthUserGateway authUserGateway)
    : ICommandHandler<AuthenticateUserCommand, AuthResult<AuthUserModel>>
{
    public async Task<AuthResult<AuthUserModel>> Handle(
        AuthenticateUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = AuthInput.NormalizeEmail(request.Email);
        var user = await authUserGateway.FindByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            return InvalidCredentials();
        }

        var verificationResult = await authUserGateway.VerifyPasswordAsync(
            user.Id,
            request.Password,
            lockoutOnFailure: true,
            cancellationToken);

        return verificationResult.Succeeded
            ? AuthResult<AuthUserModel>.Success(user)
            : InvalidCredentials();
    }

    private static AuthResult<AuthUserModel> InvalidCredentials()
    {
        return AuthResult<AuthUserModel>.Failure(
            AuthFailureKind.Unauthorized,
            "invalid_credentials",
            "Invalid email or password.");
    }
}
