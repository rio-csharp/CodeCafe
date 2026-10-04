using System.Text.Json.Serialization;
using CodeCafe.Application;
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
        // Suppressing the invalid-model-state filter keeps the 501 contract tests (empty JSON
        // bodies) reachable; validation will live in a MediatR pipeline behavior instead.
        services.AddControllers(options =>
            {
                options.Filters.Add(new ResultStatusCodeFilter());
                options.Conventions.Add(new ApiErrorResponsesConvention());
            })
            // Enums travel as strings on the wire, both in DTOs and in the error envelope.
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
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
