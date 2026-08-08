namespace CodeCafe.Application.Identity;

public enum AuthFailureKind
{
    Validation,
    Unauthorized,
    Forbidden,
    Conflict,
    NotFound
}

public sealed record AuthError(
    AuthFailureKind Kind,
    string Code,
    string Message);

public sealed class AuthResult<T>
{
    public bool Succeeded => Error is null;

    public T? Value { get; init; }

    public AuthError? Error { get; init; }

    public static AuthResult<T> Success(T value) =>
        new()
        {
            Value = value
        };

    public static AuthResult<T> Failure(AuthFailureKind kind, string code, string message) =>
        new()
        {
            Error = new AuthError(kind, code, message)
        };
}
