using System.Text.Json.Serialization;
using CodeCafe.Application;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Host.Mcp;
using CodeCafe.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Hosting;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafe(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
        });
        services.AddControllers(options =>
            {
                options.Filters.Add(new ResultStatusCodeFilter());
                options.Conventions.Add(new ApiResponsesConvention());
            })
            // Enums travel as strings on the wire, both in DTOs and in the error envelope.
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .SelectMany(entry => entry.Value?.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? entry.Key
                            : $"{entry.Key}: {error.ErrorMessage}") ?? [])
                    .ToList();
                var message = details.Count == 0
                    ? "The request body is invalid."
                    : string.Join("; ", details);
                return new BadRequestObjectResult(Result.Failure(
                    new Error("validation_error", message, ErrorKind.Validation)));
            });
        services.AddCodeCafeApplication();
        services.AddCodeCafeInfrastructure(configuration);
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddScoped<IChangeSourceAccessor, AmbientChangeSource>();
        services.AddCodeCafeRateLimiter();
        services.AddCodeCafeAuthentication();
        services.AddCodeCafeCors(configuration, environment);
        services.AddCodeCafeForwardedHeaders(configuration);
        services.AddCodeCafeShutdown(configuration);
        services.AddCodeCafeHealthChecks();
        services.AddCodeCafeMcp(configuration);
        return services;
    }
}
