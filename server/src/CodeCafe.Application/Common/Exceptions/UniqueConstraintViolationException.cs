namespace CodeCafe.Application.Common.Exceptions;

// The database is the only authority on uniqueness: a concurrent writer can take a value between
// a handler's pre-check and its save. Persistence translates the provider-specific violation into
// this, so handlers can answer with the same error their pre-check would have returned instead of
// letting an expected conflict escape as a 500.
public sealed class UniqueConstraintViolationException(string constraintName)
    : Exception($"Unique constraint violated: {constraintName}")
{
    public string ConstraintName { get; } = constraintName;
}
