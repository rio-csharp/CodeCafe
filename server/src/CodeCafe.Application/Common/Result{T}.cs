namespace CodeCafe.Application.Common;

public sealed record Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, Error? error) : base(error) => Value = value;
}
