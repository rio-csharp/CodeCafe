namespace CodeCafe.Application.Common.Exceptions;

// Persistence detected that a row changed between the handler's read and its save (optimistic
// concurrency token mismatch), so the handler can answer with the conflict error its pre-check
// would have returned instead of letting an expected race escape as a 500.
public sealed class ConcurrencyConflictException()
    : Exception("The row was modified concurrently; the save was rejected.");
