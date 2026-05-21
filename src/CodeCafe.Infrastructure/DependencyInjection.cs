using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Application.Notes;
using CodeCafe.Infrastructure.Notes;
using CodeCafe.Infrastructure.Persistence;
using CodeCafe.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodeCafe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) && !environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
        }

        connectionString ??= "Host=localhost;Database=codecafe_testing_placeholder";

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<INotebookQueryService, NotebookQueryService>();
        services.AddScoped<INotebookCommandService, NotebookCommandService>();
        services.AddScoped<INotebookFavoriteService, NotebookFavoriteService>();
        services.AddSingleton<ITipTapPlainTextExtractor, TipTapPlainTextExtractor>();
        services.AddSingleton<ITipTapContentService, TipTapContentService>();

        return services;
    }
}
