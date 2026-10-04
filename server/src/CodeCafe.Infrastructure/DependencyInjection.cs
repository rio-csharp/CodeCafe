using System.Text;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Infrastructure.Ai;
using CodeCafe.Infrastructure.Auth;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                options => Encoding.UTF8.GetByteCount(options.Jwt.SigningKey) >= 32,
                "Auth:Jwt:SigningKey must be at least 32 bytes (UTF-8)."
            )
            .Validate(
                options => options.AccessTokenLifetimeMinutes > 0,
                "Auth:AccessTokenLifetimeMinutes must be greater than zero."
            )
            .Validate(
                options => options.RefreshTokenLifetimeDays > 0,
                "Auth:RefreshTokenLifetimeDays must be greater than zero."
            )
            .ValidateOnStart();

        services.AddOptions<AiOptions>().Bind(configuration.GetSection(AiOptions.SectionName));
        // Handlers take the plain record; IOptions is only the binding/validation vehicle.
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<AiOptions>>().Value);
        services.AddSingleton<IAiChatClientFactory, AiChatClientFactory>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INotebookRepository, NotebookRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IBlockRevisionRepository, BlockRevisionRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
