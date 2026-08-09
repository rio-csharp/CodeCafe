using CodeCafe.Host.Common;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace CodeCafe.Host.Common;

/// <summary>
/// Periodically prunes expired OpenIddict tokens and authorizations so the
/// OpenIddictTokens/OpenIddictAuthorizations tables do not grow without bound.
/// <see cref="DynamicClientCleanupHostedService"/> only collects stale dynamic
/// clients; tokens issued to the static SPA client accumulate forever otherwise.
/// </summary>
public sealed class OpenIddictPruningHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OpenIddictPruningOptions> options,
    ILogger<OpenIddictPruningHostedService> logger)
    : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.Value.IntervalHours));
        do
        {
            try
            {
                await PruneAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "OpenIddict token/authorization pruning failed; retrying at the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task PruneAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tokenManager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var authorizationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();

        var threshold = DateTimeOffset.UtcNow - TimeSpan.FromDays(options.Value.PruneThresholdDays);
        await tokenManager.PruneAsync(threshold, cancellationToken);
        await authorizationManager.PruneAsync(threshold, cancellationToken);

        logger.LogInformation("Pruned OpenIddict tokens and authorizations older than {Threshold:u}.", threshold);
    }
}
