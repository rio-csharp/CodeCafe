using CodeCafe.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeCafe.Modules.Ai.Edits;

/// <summary>
/// Removes expired edit proposals even when no new AI edit is created.
/// </summary>
public sealed class AiNotebookEditProposalCleanupService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<AiNotebookEditProposalCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

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
                logger.LogWarning(exception, "Expired AI edit proposal cleanup failed; retrying at the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deleted = await dbContext.AiEditProposals
            .Where(proposal => proposal.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation("Removed {Count} expired AI edit proposals.", deleted);
        }
    }
}
