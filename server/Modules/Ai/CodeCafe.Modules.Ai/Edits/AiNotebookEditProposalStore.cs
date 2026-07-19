using System.Text.Json;
using CodeCafe.Shared.Domain.Ai;
using CodeCafe.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CodeCafe.Modules.Ai.Edits;

public interface IAiNotebookEditProposalStore
{
    AiNotebookEditProposal Save(AiNotebookEditProposal proposal);

    bool TryGet(Guid proposalId, Guid actorId, out AiNotebookEditProposal proposal);

    void Remove(Guid proposalId);
}

public sealed class DatabaseAiNotebookEditProposalStore(ApplicationDbContext dbContext) : IAiNotebookEditProposalStore
{
    public AiNotebookEditProposal Save(AiNotebookEditProposal proposal)
    {
        PruneExpiredProposals();

        dbContext.AiEditProposals.Add(new AiEditProposal
        {
            Id = proposal.ProposalId,
            ActorUserId = proposal.ActorId,
            NotebookId = proposal.NotebookId,
            NotebookSlug = proposal.NotebookSlug,
            PayloadJson = JsonSerializer.Serialize(proposal),
            ExpiresAtUtc = proposal.ExpiresAtUtc
        });
        dbContext.SaveChanges();

        return proposal;
    }

    public bool TryGet(Guid proposalId, Guid actorId, out AiNotebookEditProposal proposal)
    {
        var entry = dbContext.AiEditProposals
            .AsNoTracking()
            .SingleOrDefault(existingProposal => existingProposal.Id == proposalId);
        if (entry is not null
            && entry.ActorUserId == actorId
            && entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            proposal = JsonSerializer.Deserialize<AiNotebookEditProposal>(entry.PayloadJson)!;
            return true;
        }

        proposal = default!;
        return false;
    }

    public void Remove(Guid proposalId)
    {
        dbContext.AiEditProposals
            .Where(existingProposal => existingProposal.Id == proposalId)
            .ExecuteDelete();
    }

    private void PruneExpiredProposals()
    {
        var now = DateTimeOffset.UtcNow;

        // DateTimeOffset range comparisons are not translatable on every provider (e.g. SQLite),
        // so expired rows are filtered client-side and deleted by key.
        var expiredIds = dbContext.AiEditProposals
            .Select(existingProposal => new { existingProposal.Id, existingProposal.ExpiresAtUtc })
            .AsEnumerable()
            .Where(existingProposal => existingProposal.ExpiresAtUtc <= now)
            .Select(existingProposal => existingProposal.Id)
            .ToList();
        if (expiredIds.Count == 0)
        {
            return;
        }

        dbContext.AiEditProposals
            .Where(existingProposal => expiredIds.Contains(existingProposal.Id))
            .ExecuteDelete();
    }
}

public sealed class MemoryAiNotebookEditProposalStore(IMemoryCache cache) : IAiNotebookEditProposalStore
{
    private const string CacheKeyPrefix = "ai-note-edit-proposal:";

    public AiNotebookEditProposal Save(AiNotebookEditProposal proposal)
    {
        cache.Set(
            BuildKey(proposal.ProposalId),
            proposal,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = proposal.ExpiresAtUtc
            });

        return proposal;
    }

    public bool TryGet(Guid proposalId, Guid actorId, out AiNotebookEditProposal proposal)
    {
        if (cache.TryGetValue<AiNotebookEditProposal>(BuildKey(proposalId), out var cached)
            && cached is not null
            && cached.ActorId == actorId
            && cached.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            proposal = cached;
            return true;
        }

        proposal = default!;
        return false;
    }

    public void Remove(Guid proposalId)
        => cache.Remove(BuildKey(proposalId));

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
