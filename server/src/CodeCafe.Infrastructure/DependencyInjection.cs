using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Infrastructure.Auth;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(
                options => options.Jwt.SigningKey.Length >= 32,
                "Auth:Jwt:SigningKey must be at least 32 characters."
            )
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INotebookRepository, NotebookRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
