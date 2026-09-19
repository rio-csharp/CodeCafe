using CodeCafe.Application.Common;

namespace CodeCafe.Application.Auth;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyRegistered = new(
        "email_already_registered",
        "A user with this email already exists.",
        ErrorKind.Conflict
    );
}
