using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Notes;

public sealed class NotebookShare : Entity, IAuditableEntity
{
    private NotebookShare() { }

    private NotebookShare(
        Guid id,
        Guid notebookId,
        Guid userId,
        Guid grantedByUserId,
        DateTimeOffset createdAtUtc
    )
    {
        Id = id;
        NotebookId = notebookId;
        UserId = userId;
        GrantedByUserId = grantedByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid NotebookId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid GrantedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static NotebookShare Create(
        Guid id,
        Guid notebookId,
        Guid userId,
        Guid grantedByUserId,
        DateTimeOffset now
    ) => new(id, notebookId, userId, grantedByUserId, now);
}
