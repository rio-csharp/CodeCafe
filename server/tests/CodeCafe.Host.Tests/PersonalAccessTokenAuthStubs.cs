using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Domain.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeCafe.Host.Tests;

// Exercises the real authentication handler's PAT path hermetically: the two repositories it
// resolves are swapped for in-memory stubs, so no PostgreSQL is needed. Shared so that endpoint
// tests outside the auth area can sign in without repeating the wiring.
internal static class PersonalAccessTokenAuthStubs
{
    internal static WebApplicationFactory<Program> WithPersonalAccessTokenRepositories(
        this WebApplicationFactory<Program> factory,
        PersonalAccessToken? token,
        User? user) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPersonalAccessTokenRepository>();
                services.RemoveAll<IUserRepository>();
                services.AddScoped<IPersonalAccessTokenRepository>(_ => new StubPersonalAccessTokenRepository(token));
                services.AddScoped<IUserRepository>(_ => new StubUserRepository(user));
            }));
}

internal sealed class StubPersonalAccessTokenRepository(PersonalAccessToken? token) : IPersonalAccessTokenRepository
{
    public Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<PersonalAccessToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(token is not null && token.TokenHash == tokenHash ? token : null);

    public Task<IReadOnlyList<PersonalAccessToken>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PersonalAccessToken>>(
            token is not null && token.UserId == userId ? [token] : []);
}

internal sealed class StubUserRepository(User? user) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(user is not null && user.NormalizedEmail == normalizedEmail ? user : null);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(user is not null && user.Id == id ? user : null);

    public Task<IReadOnlyList<User>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(user is not null && ids.Contains(user.Id) ? [user] : []);

    public Task AddAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;
}
