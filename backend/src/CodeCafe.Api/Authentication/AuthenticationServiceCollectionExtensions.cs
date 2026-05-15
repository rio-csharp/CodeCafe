using CodeCafe.Api.Configuration;
using CodeCafe.Application.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace CodeCafe.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthCookieManager, AuthCookieManager>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IAuthTokenValidationConfigurationProvider>((options, tokenValidationConfigurationProvider) =>
            {
                var tokenValidationConfiguration = tokenValidationConfigurationProvider.Get();
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenValidationConfiguration.SigningKey));

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
                    ClockSkew = tokenValidationConfiguration.ClockSkew,
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
