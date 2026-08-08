using CodeCafe.Domain.Ai;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CodeCafe.Modules.Ai.Edits;

public interface IAiNotebookEditProposalStore
{
    Task<AiNotebookEditProposal> SaveAsync(AiNotebookEditProposal proposal, CancellationToken cancellationToken);

    Task<AiNotebookEditProposal?> TryGetAsync(Guid proposalId, Guid actorId, CancellationToken cancellationToken);

    Task<AiNotebookEditProposal?> TryConsumeAsync(Guid proposalId, Guid actorId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid proposalId, CancellationToken cancellationToken);
}

public sealed class DatabaseAiNotebookEditProposalStore(ApplicationDbContext dbContext) : IAiNotebookEditProposalStore
{
    public async Task<AiNotebookEditProposal> SaveAsync(
        AiNotebookEditProposal proposal,
        CancellationToken cancellationToken)
    {
        await PruneExpiredProposalsAsync(cancellationToken);

        dbContext.AiEditProposals.Add(new AiEditProposal
        {
            Id = proposal.ProposalId,
            ActorUserId = proposal.ActorId,
            NotebookId = proposal.NotebookId,
            NotebookSlug = proposal.NotebookSlug,
            PayloadJson = JsonSerializer.Serialize(proposal),
            ExpiresAtUtc = proposal.ExpiresAtUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return proposal;
    }

    public async Task<AiNotebookEditProposal?> TryGetAsync(
        Guid proposalId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.AiEditProposals
            .AsNoTracking()
            .SingleOrDefaultAsync(existingProposal => existingProposal.Id == proposalId, cancellationToken);
        if (entry is not null
            && entry.ActorUserId == actorId
            && entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return JsonSerializer.Deserialize<AiNotebookEditProposal>(entry.PayloadJson)!;
        }

        return null;
    }

    public async Task<AiNotebookEditProposal?> TryConsumeAsync(
        Guid proposalId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.AiEditProposals
            .AsNoTracking()
            .SingleOrDefaultAsync(existingProposal => existingProposal.Id == proposalId, cancellationToken);
        if (entry is null
            || entry.ActorUserId != actorId
            || entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        // The conditional delete is the atomic claim: when two applies race, exactly one
        // deletes a row and wins; the loser observes 0 deleted rows and gets null.
        var deleted = await dbContext.AiEditProposals
            .Where(existingProposal => existingProposal.Id == proposalId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 1
            ? JsonSerializer.Deserialize<AiNotebookEditProposal>(entry.PayloadJson)
            : null;
    }

    public async Task RemoveAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        await dbContext.AiEditProposals
            .Where(existingProposal => existingProposal.Id == proposalId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task PruneExpiredProposalsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (string.Equals(dbContext.Database.ProviderName, DatabaseProviderNames.Npgsql, StringComparison.Ordinal))
        {
            await dbContext.AiEditProposals
                .Where(existingProposal => existingProposal.ExpiresAtUtc <= now)
                .ExecuteDeleteAsync(cancellationToken);
            return;
        }

        // DateTimeOffset range comparisons are not translatable on every provider (e.g. SQLite),
        // so expired rows are filtered client-side and deleted by key there.
        var candidates = await dbContext.AiEditProposals
            .Select(existingProposal => new { existingProposal.Id, existingProposal.ExpiresAtUtc })
            .ToListAsync(cancellationToken);
        var expiredIds = candidates
            .Where(existingProposal => existingProposal.ExpiresAtUtc <= now)
            .Select(existingProposal => existingProposal.Id)
            .ToList();
        if (expiredIds.Count == 0)
        {
            return;
        }

        await dbContext.AiEditProposals
            .Where(existingProposal => expiredIds.Contains(existingProposal.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class MemoryAiNotebookEditProposalStore(IMemoryCache cache) : IAiNotebookEditProposalStore
{
    private const string CacheKeyPrefix = "ai-note-edit-proposal:";

    public Task<AiNotebookEditProposal> SaveAsync(
        AiNotebookEditProposal proposal,
        CancellationToken cancellationToken)
    {
        cache.Set(
            BuildKey(proposal.ProposalId),
            proposal,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = proposal.ExpiresAtUtc
            });

        return Task.FromResult(proposal);
    }

    public Task<AiNotebookEditProposal?> TryGetAsync(
        Guid proposalId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<AiNotebookEditProposal>(BuildKey(proposalId), out var cached)
            && cached is not null
            && cached.ActorId == actorId
            && cached.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<AiNotebookEditProposal?>(cached);
        }

        return Task.FromResult<AiNotebookEditProposal?>(null);
    }

    public Task<AiNotebookEditProposal?> TryConsumeAsync(
        Guid proposalId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        // Test-only store (registered by ServerTestFactory): TryGetValue + Remove is a deliberate
        // check-then-act, not an atomic claim; do not copy this pattern into a production path.
        if (cache.TryGetValue<AiNotebookEditProposal>(BuildKey(proposalId), out var cached)
            && cached is not null
            && cached.ActorId == actorId
            && cached.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            cache.Remove(BuildKey(proposalId));
            return Task.FromResult<AiNotebookEditProposal?>(cached);
        }

        return Task.FromResult<AiNotebookEditProposal?>(null);
    }

    public Task RemoveAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        cache.Remove(BuildKey(proposalId));
        return Task.CompletedTask;
    }

    private static string BuildKey(Guid proposalId)
        => CacheKeyPrefix + proposalId.ToString("N");
}

public sealed record AiNotebookEditProposal(
    Guid ProposalId,
    Guid ActorId,
    string RequestedOperation,
    string EffectiveOperation,
    string Mode,
    Guid NotebookId,
    string NotebookSlug,
    string NotebookTitle,
    Guid? PageId,
    string Title,
    string? PagePath,
    string? ParentPath,
    JsonElement? BeforeContentJson,
    string? BeforePlainTextContent,
    JsonElement AfterContentJson,
    string? AfterPlainTextContent,
    JsonElement? OperationsJson,
    DateTimeOffset SourcePageUpdatedAtUtc,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Summary);
