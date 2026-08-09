using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace CodeCafe.Infrastructure.Identity;

/// <summary>
/// Prunes expired OpenIddict tokens and authorizations on a daily schedule. Without this,
/// the tokens and authorizations tables grow unbounded as clients request new tokens.
/// </summary>
public sealed class OpenIddictTokenCleanupService(
    IServiceProvider serviceProvider,
    ILogger<OpenIddictTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 5 minutes after startup before the first prune to avoid competing with
        // EF migrations or seeding operations at app launch.
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PruneExpiredTokensAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "OpenIddict token cleanup failed. Will retry in {Interval}.", CheckInterval);
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task PruneExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var authorizationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();

        // Prune tokens that expired more than 14 days ago; keeping a buffer allows
        // debugging recent authentication issues without losing the token trail.
        var threshold = DateTimeOffset.UtcNow.AddDays(-14);
        var prunedTokens = await tokenManager.PruneAsync(threshold, cancellationToken);

        // Prune authorizations that have no remaining valid tokens attached; orphaned
        // authorizations serve no purpose and only consume database space.
        var prunedAuthorizations = await authorizationManager.PruneAsync(threshold, cancellationToken);

        if (prunedTokens > 0 || prunedAuthorizations > 0)
        {
            logger.LogInformation(
                "OpenIddict cleanup completed. Pruned {TokenCount} tokens and {AuthorizationCount} authorizations.",
                prunedTokens,
                prunedAuthorizations);
        }
    }
}
