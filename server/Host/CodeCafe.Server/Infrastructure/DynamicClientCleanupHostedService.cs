using CodeCafe.Modules.Identity.Presentation.Auth;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
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

    internal async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

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

        var cutoff = (DateTimeOffset.UtcNow - StaleAfter).UtcDateTime;

        // Stale = has tokens and none of them is still valid (or never expires).
        var staleIds = await dbContext.Set<OpenIddictEntityFrameworkCoreToken<Guid>>()
            .Where(token => token.Application != null && dynamicClientIds.Contains(token.Application.Id))
            .GroupBy(token => token.Application!.Id)
            .Where(group => !group.Any(token => token.ExpirationDate == null)
                && group.Max(token => token.ExpirationDate) < cutoff)
            .Select(group => group.Key)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        var failed = 0;
        foreach (var staleId in staleIds)
        {
            // Isolate failures per client: one poisoned client must not block the rest of
            // the sweep (and every run after it, since it would stay first in line forever).
            try
            {
                var application = await applicationManager.FindByIdAsync(staleId.ToString(), cancellationToken);
                if (application is null)
                {
                    continue;
                }

                // The token/authorization foreign keys are NO ACTION, so a bare row delete would
                // violate them; the OpenIddict store removes the dependent rows itself.
                await applicationManager.DeleteAsync(application, cancellationToken);
                deleted++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                logger.LogWarning(exception, "Failed to remove stale dynamically registered OAuth client {ApplicationId}; continuing with the remaining clients.", staleId);
            }
        }

        logger.LogInformation("Removed {DeletedCount} stale dynamically registered OAuth clients ({FailedCount} failed).", deleted, failed);
    }
}
