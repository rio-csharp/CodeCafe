namespace CodeCafe.Shared.Application.Common;

/// <summary>
/// Marks an exception (via <see cref="Exception.Data"/>) as already logged by the
/// MediatR pipeline (<c>LoggingBehavior</c>), so the HTTP boundary
/// (<c>GlobalExceptionHandler</c>) does not log the same exception a second time.
/// Trade-off: the single Error entry lives in <c>LoggingBehavior</c>, which carries
/// the richer context (request name + elapsed time); exceptions thrown outside the
/// MediatR pipeline are still logged by the exception handler.
/// </summary>
public static class ExceptionLoggingMarker
{
    private const string LoggedKey = "CodeCafe.ExceptionAlreadyLogged";

    public static void MarkAsLogged(Exception exception) => exception.Data[LoggedKey] = true;

    public static bool IsMarkedAsLogged(Exception exception) => exception.Data[LoggedKey] is true;
}
