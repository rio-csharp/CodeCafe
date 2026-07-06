using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace CodeCafe.Ai.Edits;

public interface IAiNotebookEditProposalStore
{
    AiNotebookEditProposal Save(AiNotebookEditProposal proposal);

    bool TryGet(Guid proposalId, Guid actorId, out AiNotebookEditProposal proposal);

    void Remove(Guid proposalId);
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
