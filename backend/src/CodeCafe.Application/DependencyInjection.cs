using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Application;

using CodeCafe.Application.Ai;
using CodeCafe.Application.Notes;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeApplication(this IServiceCollection services)
    {
        services.AddScoped<IAiProviderConfigurationService, AiProviderConfigurationService>();
        services.AddScoped<INotesSettingsService, NotesSettingsService>();

        return services;
    }
}
