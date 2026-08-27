using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(IAuthUserGateway authUserGateway)
    : ICommandHandler<RegisterUserCommand, AuthResult<AuthUserModel>>
{
    public async Task<AuthResult<AuthUserModel>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken
    )
    {
        if (!request.RegistrationEnabled)
        {
            return AuthResult<AuthUserModel>.Failure(
                AuthFailureKind.Forbidden,
                "registration_disabled",
                "Registration is currently disabled."
            );
        }

        var email = AuthInput.NormalizeEmail(request.Email);
        var displayName = AuthInput.NormalizeDisplayName(request.DisplayName);

        var existingUser = await authUserGateway.FindByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            return AuthResult<AuthUserModel>.Failure(
                AuthFailureKind.Conflict,
                "email_already_registered",
                "A user with this email already exists."
            );
        }

        var createUserResult = await authUserGateway.CreateUserAsync(
            email,
            displayName,
            request.Password,
            cancellationToken
        );

        if (!createUserResult.Succeeded)
        {
            return createUserResult.ErrorCodes.Any(IsDuplicateUserError)
                ? AuthResult<AuthUserModel>.Failure(
                    AuthFailureKind.Conflict,
                    "email_already_registered",
                    "A user with this email already exists."
                )
                : AuthResult<AuthUserModel>.Failure(
                    AuthFailureKind.Validation,
                    "registration_failed",
                    "Registration failed. Please check the submitted values."
                );
        }

        return AuthResult<AuthUserModel>.Success(createUserResult.User!);
    }

    private static bool IsDuplicateUserError(string errorCode)
    {
        return string.Equals(errorCode, "DuplicateEmail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(errorCode, "DuplicateUserName", StringComparison.OrdinalIgnoreCase);
    }
}
