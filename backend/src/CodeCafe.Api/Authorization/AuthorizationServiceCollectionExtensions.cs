using CodeCafe.Api.Configuration;

namespace CodeCafe.Api.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, NotesSettingsEditRequirementHandler>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(
                ApiPolicyNames.EditNotesSettings,
                policy => policy.AddRequirements(NotesSettingsEditRequirement.Instance));

        return services;
    }
}
