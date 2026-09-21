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

    internal static NotebookShare Create(Guid notebookId, Guid userId, CollaboratorRole role)
        => new(Guid.CreateVersion7(), notebookId, userId, role);

    internal void ChangeRole(CollaboratorRole role) => Role = role;
}
