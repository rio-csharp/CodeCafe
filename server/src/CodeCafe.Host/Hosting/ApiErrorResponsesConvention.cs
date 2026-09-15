using CodeCafe.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Hosting;

// The ResultStatusCodeFilter rewrites error statuses at runtime, which ApiExplorer cannot see;
// this convention publishes the same information so the OpenAPI document lists every error
// status a contract operation can return. Endpoints returning IResult (export, AI chat)
// document themselves with attributes instead.
internal sealed class ApiErrorResponsesConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var action in application.Controllers.SelectMany(controller => controller.Actions))
        {
            if (!typeof(Result).IsAssignableFrom(UnwrapTask(action.ActionMethod.ReturnType)))
                continue;

            action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status400BadRequest));

            var allowsAnonymous = action.Attributes.OfType<AllowAnonymousAttribute>().Any()
                || action.Controller.Attributes.OfType<AllowAnonymousAttribute>().Any();
            if (!allowsAnonymous)
            {
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status401Unauthorized));
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status403Forbidden));
            }

            var hasRouteParameter = action.Selectors.Any(selector =>
                selector.AttributeRouteModel?.Template?.Contains('{') == true);
            if (hasRouteParameter)
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status404NotFound));

            var isReadOnly = action.Attributes.OfType<HttpMethodAttribute>().Any()
                && action.Attributes.OfType<HttpMethodAttribute>()
                    .All(method => method.HttpMethods.All(verb => verb is "GET" or "HEAD"));
            if (!isReadOnly)
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status409Conflict));

            if (action.Attributes.OfType<EnableRateLimitingAttribute>().Any())
                action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status429TooManyRequests));

            action.Filters.Add(new ProducesResponseTypeAttribute(typeof(Result), StatusCodes.Status500InternalServerError));
        }
    }

    private static Type UnwrapTask(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
            ? type.GetGenericArguments()[0]
            : type;
}
