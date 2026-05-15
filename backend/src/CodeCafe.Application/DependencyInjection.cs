using CodeCafe.Application.Auth;
using CodeCafe.Application.Notes;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotesSettingsService, NotesSettingsService>();
        services.AddScoped<INotesService, NotesService>();

        return services;
    }
}
