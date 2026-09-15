using CodeCafe.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace CodeCafe.Host.Hosting;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, message) = exception switch
        {
            NotImplementedException => (StatusCodes.Status501NotImplemented, "not_implemented", "This operation is not implemented yet."),
            _ => (StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred.")
        };

        await ErrorResponses.WriteAsync(
            httpContext, statusCode, code, message, ErrorKind.Unexpected, cancellationToken);
        return true;
    }
}
