namespace CodeCafe.Application.Identity;

public interface IAuthUserGateway
{
    Task<AuthUserModel?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken
    );

    Task<AuthUserModel?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<AuthCreateUserResult> CreateUserAsync(
        string normalizedEmail,
        string displayName,
        string password,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Verifies the password for the account with the given email in a single
    /// user lookup, so the login path does not load the user twice.
    /// </summary>
    Task<AuthPasswordVerificationResult> VerifyPasswordAsync(
        string normalizedEmail,
        string password,
        bool lockoutOnFailure,
        CancellationToken cancellationToken
    );
}

public sealed record AuthCreateUserResult(
    bool Succeeded,
    AuthUserModel? User,
    IReadOnlyList<string> ErrorCodes
)
{
    public static AuthCreateUserResult Success(AuthUserModel user) =>
        new(true, user, Array.Empty<string>());

    public static AuthCreateUserResult Failure(IReadOnlyList<string> errorCodes) =>
        new(false, null, errorCodes);
}

public sealed record AuthPasswordVerificationResult(
    bool Succeeded,
    bool IsLockedOut,
    AuthUserModel? User
)
{
    public static AuthPasswordVerificationResult Success(AuthUserModel user) =>
        new(true, false, user);

    public static AuthPasswordVerificationResult Failure(bool isLockedOut) =>
        new(false, isLockedOut, null);
}
