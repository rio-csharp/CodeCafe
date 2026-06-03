namespace CodeCafe.Application.Auth;

public interface IAuthUserGateway
{
    Task<AuthUserModel?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<AuthUserModel?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<AuthCreateUserResult> CreateUserAsync(
        string normalizedEmail,
        string displayName,
        string password,
        CancellationToken cancellationToken);

    Task<AuthPasswordVerificationResult> VerifyPasswordAsync(
        Guid userId,
        string password,
        bool lockoutOnFailure,
        CancellationToken cancellationToken);
}

public sealed record AuthCreateUserResult(
    bool Succeeded,
    AuthUserModel? User,
    IReadOnlyList<string> ErrorCodes)
{
    public static AuthCreateUserResult Success(AuthUserModel user) =>
        new(true, user, Array.Empty<string>());

    public static AuthCreateUserResult Failure(IReadOnlyList<string> errorCodes) =>
        new(false, null, errorCodes);
}

public sealed record AuthPasswordVerificationResult(
    bool Succeeded,
    bool IsLockedOut)
{
    public static AuthPasswordVerificationResult Success() => new(true, false);

    public static AuthPasswordVerificationResult Failure(bool isLockedOut) => new(false, isLockedOut);
}
