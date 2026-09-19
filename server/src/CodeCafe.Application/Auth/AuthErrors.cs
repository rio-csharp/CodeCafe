using CodeCafe.Application.Common;

namespace CodeCafe.Application.Auth;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyRegistered = new(
        "email_already_registered",
        "A user with this email already exists.",
        ErrorKind.Conflict
    );

    // One uniform failure for unknown email and wrong password, so the response
    // cannot be used to probe which emails are registered.
    public static readonly Error InvalidCredentials = new(
        "invalid_credentials",
        "Invalid email or password.",
        ErrorKind.Unauthorized
    );
}
