namespace CodeCafe.Application.Common;

public record Result
{
    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    protected Result(Error? error) => Error = error;

    public static Result Success() => new((Error?)null);

    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result Failure(Error error) => new(error);

    public static Result<T> Failure<T>(Error error) => new(default, error);
}
