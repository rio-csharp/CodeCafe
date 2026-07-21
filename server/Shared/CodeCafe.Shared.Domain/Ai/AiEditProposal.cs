using CodeCafe.Shared.Domain.Common.Interfaces;

namespace CodeCafe.Shared.Domain.Ai;

public sealed class AiEditProposal : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid ActorUserId { get; set; }

    public Guid NotebookId { get; set; }

    public required string NotebookSlug { get; set; }

    public required string PayloadJson { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
