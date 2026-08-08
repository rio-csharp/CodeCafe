using System.Text.Json;
using CodeCafe.Domain.Ai;

namespace CodeCafe.Application.Ai;

public interface IAiNotebookEditProposalStore
{
    Task<AiNotebookEditProposal> SaveAsync(AiNotebookEditProposal proposal, CancellationToken cancellationToken);

    Task<AiNotebookEditProposal?> TryGetAsync(Guid proposalId, Guid actorId, CancellationToken cancellationToken);

    Task<AiNotebookEditProposal?> TryConsumeAsync(Guid proposalId, Guid actorId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid proposalId, CancellationToken cancellationToken);
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
