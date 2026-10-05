using CodeCafe.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Hosting;

// Publishes the responses that ResultStatusCodeFilter produces at runtime but ApiExplorer
// cannot see. Success is always 200 with the Result<T> envelope (or bare Result); errors
// carry the Result envelope under the ErrorKind statuses. Endpoints returning IResult
// (export, AI chat) document themselves with attributes.
internal sealed class ApiResponsesConvention : IApplicationModelConvention
{
    private const string JsonContentType = "application/json";

    public void Apply(ApplicationModel application)
    {
        foreach (var action in application.Controllers.SelectMany(controller => controller.Actions))
        {
            var returnType = UnwrapTask(action.ActionMethod.ReturnType);
            if (!typeof(Result).IsAssignableFrom(returnType))
                continue;

            action.Filters.Add(new ProducesResponseTypeAttribute(returnType, StatusCodes.Status200OK, JsonContentType));
            action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status400BadRequest, JsonContentType));

            var allowsAnonymous = action.Attributes.OfType<AllowAnonymousAttribute>().Any()
                || action.Controller.Attributes.OfType<AllowAnonymousAttribute>().Any();
            if (!allowsAnonymous)
            {
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status401Unauthorized, JsonContentType));
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status403Forbidden, JsonContentType));
            }

            var hasRouteParameter = action.Selectors.Any(selector =>
                selector.AttributeRouteModel?.Template?.Contains('{') == true);
            if (hasRouteParameter)
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status404NotFound, JsonContentType));

            var isReadOnly = action.Attributes.OfType<HttpMethodAttribute>().Any()
                && action.Attributes.OfType<HttpMethodAttribute>()
                    .All(method => method.HttpMethods.All(verb => verb is "GET" or "HEAD"));
            if (!isReadOnly)
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status409Conflict, JsonContentType));

            if (action.Attributes.OfType<EnableRateLimitingAttribute>().Any())
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status429TooManyRequests, JsonContentType));

            action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status500InternalServerError, JsonContentType));
        }
    }

    private static Type UnwrapTask(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
            ? type.GetGenericArguments()[0]
            : type;
}
