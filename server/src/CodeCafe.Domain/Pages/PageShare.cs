using CodeCafe.Domain.Primitives;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Domain.Pages;

public sealed class PageShare : Entity
{
    private PageShare()
        : base(Guid.Empty) { }

    private PageShare(Guid id, Guid pageId, Guid userId, CollaboratorRole role)
        : base(id)
    {
        PageId = pageId;
        UserId = userId;
        Role = role;
    }

    public Guid PageId { get; private set; }

    public Guid UserId { get; private set; }

    public CollaboratorRole Role { get; private set; }

    // No client-assigned id, same reasoning as NotebookShare.Create.
    internal static PageShare Create(Guid pageId, Guid userId, CollaboratorRole role)
        => new(Guid.Empty, pageId, userId, role);

    internal void ChangeRole(CollaboratorRole role) => Role = role;
}
