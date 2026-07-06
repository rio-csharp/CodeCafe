namespace CodeCafe.Application.Notes;

public enum NotesFailureKind
{
    Validation,
    Forbidden,
    NotFound,
    Conflict
}

public sealed record NotesError(
    NotesFailureKind Kind,
    string Code,
    string Message,
    string? Field = null,
    IReadOnlyDictionary<string, object?>? Details = null);

public sealed class NotesResult
{
    public bool Succeeded => Error is null;

    public NotesError? Error { get; init; }

    public static NotesResult Success() => new();

    public static NotesResult Failure(
        NotesFailureKind kind,
        string code,
        string message,
        string? field = null,
        IReadOnlyDictionary<string, object?>? details = null) =>
        new()
        {
            Error = new NotesError(kind, code, message, field, details)
        };
}

public sealed class NotesResult<T>
{
    public bool Succeeded => Error is null;

    public T? Value { get; init; }

    public NotesError? Error { get; init; }

    public static NotesResult<T> Success(T value) =>
        new()
        {
            Value = value
        };

    public static NotesResult<T> Failure(
        NotesFailureKind kind,
        string code,
        string message,
        string? field = null,
        IReadOnlyDictionary<string, object?>? details = null) =>
        new()
        {
            Error = new NotesError(kind, code, message, field, details)
        };
}
