using CodeCafe.Shared.Infrastructure.Persistence;
using CodeCafe.Modules.Identity.Presentation.Auth;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace CodeCafe.Server.Infrastructure;

/// <summary>
/// Periodically removes dynamically registered OAuth clients (DCR) so their
/// count stays bounded. A client is stale when it owns at least one token and
/// every token expired more than <see cref="StaleAfter"/> ago. Clients with no
/// tokens at all are left alone so fresh registrations are never collected.
/// </summary>
public sealed class DynamicClientCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DynamicClientCleanupHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Dynamic OAuth client cleanup failed; retrying at the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var registeredClients = await dbContext.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .AsNoTracking()
            .Where(application => application.ClientId != null)
            .Select(application => new { application.Id, application.ClientId })
            .ToListAsync(cancellationToken);

        var dynamicClientIds = registeredClients
            .Where(application => OpenIddictClientRegistration.IsDynamicallyRegisteredClientId(application.ClientId))
            .Select(application => application.Id)
            .ToArray();

        if (dynamicClientIds.Length == 0)
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow - StaleAfter;

        var tokenStates = await dbContext.Set<OpenIddictEntityFrameworkCoreToken<Guid>>()
            .AsNoTracking()
            .Where(token => token.Application != null && dynamicClientIds.Contains(token.Application.Id))
            .Select(token => new { ApplicationId = token.Application!.Id, token.ExpirationDate })
            .ToListAsync(cancellationToken);

        // Stale = has tokens and none of them is still valid (or never expires).
        var staleIds = tokenStates
            .GroupBy(token => token.ApplicationId)
            .Where(group => group.All(token => token.ExpirationDate != null && token.ExpirationDate < cutoff))
            .Select(group => group.Key)
            .ToArray();

        if (staleIds.Length == 0)
        {
            return;
        }

        var deleted = await dbContext.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .Where(application => staleIds.Contains(application.Id))
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Removed {Count} stale dynamically registered OAuth clients.", deleted);
    }
}
