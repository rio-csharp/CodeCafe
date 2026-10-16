using CodeCafe.Domain.Primitives;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Domain.Notebooks;

public sealed class NotebookShare : Entity
{
    private NotebookShare()
        : base(Guid.Empty) { }

    private NotebookShare(Guid id, Guid notebookId, Guid userId, CollaboratorRole role)
        : base(id)
    {
        NotebookId = notebookId;
        UserId = userId;
        Role = role;
    }

    public Guid NotebookId { get; private set; }

    public Guid UserId { get; private set; }

    public CollaboratorRole Role { get; private set; }

    // No client-assigned id: shares enter the change tracker through the parent's collection
    // navigation, where EF classifies entities by key — a set key reads as "existing row" and
    // produces an UPDATE for a row that was never inserted. An empty key gets marked Added and
    // EF generates the value at save time. Share ids are never read by application code.
    internal static NotebookShare Create(Guid notebookId, Guid userId, CollaboratorRole role)
        => new(Guid.Empty, notebookId, userId, role);

    internal void ChangeRole(CollaboratorRole role) => Role = role;
}
