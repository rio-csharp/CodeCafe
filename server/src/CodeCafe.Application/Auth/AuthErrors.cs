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

    public static readonly Error InvalidRefreshToken = new(
        "invalid_refresh_token",
        "The refresh token is invalid or has expired.",
        ErrorKind.Unauthorized
    );

    // A valid access token can outlive its user when the account was deleted after issuance.
    public static readonly Error UserNotFound = new(
        "user_not_found",
        "The user no longer exists.",
        ErrorKind.NotFound
    );

    public static readonly Error IncorrectCurrentPassword = new(
        "incorrect_current_password",
        "The current password is incorrect.",
        ErrorKind.Unauthorized
    );

    // One uniform failure for unknown and foreign token ids, so the endpoint cannot be
    // used to probe which personal access tokens exist.
    public static readonly Error PersonalAccessTokenNotFound = new(
        "personal_access_token_not_found",
        "The personal access token was not found.",
        ErrorKind.NotFound
    );
}
