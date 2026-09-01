using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Notes;

public sealed class NotebookFavorite : Entity, IAuditableEntity
{
    private NotebookFavorite() { }

    private NotebookFavorite(Guid id, Guid notebookId, Guid userId, DateTimeOffset createdAtUtc)
    {
        Id = id;
        NotebookId = notebookId;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid NotebookId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static NotebookFavorite Create(Guid id, Guid notebookId, Guid userId, DateTimeOffset now) =>
        new(id, notebookId, userId, now);
}
