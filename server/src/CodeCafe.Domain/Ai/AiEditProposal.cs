using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Ai;

public sealed class AiEditProposal : Entity, IAuditableEntity
{
    private AiEditProposal() { }

    private AiEditProposal(
        Guid id,
        Guid actorUserId,
        Guid notebookId,
        string payloadJson,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc
    )
    {
        Id = id;
        ActorUserId = actorUserId;
        NotebookId = notebookId;
        PayloadJson = payloadJson;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ActorUserId { get; private set; }

    public Guid NotebookId { get; private set; }

    public string PayloadJson { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static AiEditProposal Create(
        Guid id,
        Guid actorUserId,
        Guid notebookId,
        string payloadJson,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset now
    ) => new(id, actorUserId, notebookId, payloadJson, expiresAtUtc, now);
}
