using CodeCafe.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace CodeCafe.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAuthCookieManager, AuthCookieManager>();
        services.AddSingleton<IAuthSessionService, AuthSessionService>();
        services.AddSingleton<IConfiguredCredentialValidator, ConfiguredCredentialValidator>();
        services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ConfiguredLoginOptions>>((options, configuredLoginOptions) =>
            {
                var configuredOptions = configuredLoginOptions.Value;
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredOptions.JwtSigningKey));

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies[ApiCookieNames.AuthCookieName];
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    },
                };
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.FromMinutes(1),
                    IssuerSigningKey = signingKey,
                    NameClaimType = ClaimTypes.Name,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                };
            });

        return services;
    }
}
