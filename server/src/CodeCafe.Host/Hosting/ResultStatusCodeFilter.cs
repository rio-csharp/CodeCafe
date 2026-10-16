using CodeCafe.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CodeCafe.Host.Hosting;

// Maps application-level errors onto HTTP status codes; without this every Result
// would serialize as 200 and clients could not distinguish failure categories.
internal sealed class ResultStatusCodeFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: Result { Error: { } error } } result)
            return;

        result.StatusCode = ErrorStatusCodes.FromKind(error.Kind);
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
