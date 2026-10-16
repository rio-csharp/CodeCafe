using CodeCafe.Domain.Primitives;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Domain.Notebooks;

public sealed class Notebook : Entity
{
    public const int MaxTitleLength = 120;
    public const int MaxDescriptionLength = 2000;
    public const int MaxSlugLength = 80;
    public const int MaxTagCount = 20;
    public const int MaxTagLength = 30;

    private List<string> _tags = [];
    private readonly List<NotebookShare> _shares = [];

    private Notebook()
        : base(Guid.Empty) { }

    private Notebook(Guid id, Guid ownerId, string title, string? description, string slug, NotebookVisibility visibility)
        : base(id)
    {
        OwnerId = ownerId;
        Title = title;
        Description = description;
        Slug = slug;
        Visibility = visibility;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    // Globally unique: notebooks are addressable by slug alone in URLs.
    public string Slug { get; private set; } = string.Empty;

    public NotebookVisibility Visibility { get; private set; }

    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    // Only the hash is stored; the raw access code is unrecoverable from the database.
    public string? AccessCodeHash { get; private set; }

    public IReadOnlyCollection<NotebookShare> Shares => _shares.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    // Soft-deleted notebooks sit in the trash until restored or purged.
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    // Head of the root page chain; null for an empty notebook. Plain id, mirroring the page chains.
    public Guid? FirstPageId { get; private set; }

    public static Notebook Create(Guid ownerId, string title, string? description, string slug, NotebookVisibility visibility)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Notebook(Guid.CreateVersion7(), ownerId, title, description, slug, visibility);
    }

    public void UpdateDetails(string title, string? description, NotebookVisibility visibility)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Description = description;
        Visibility = visibility;
        Touch();
    }

    public void ChangeSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Slug = slug;
        Touch();
    }

    // Tags are notebook content (they show in lists and drive filtering), so changing them
    // counts as an update — unlike access control (SetAccessCodeHash/Share/RevokeShare).
    public void SetTags(IEnumerable<string> tags)
    {
        var list = tags.ToList();
        if (list.Count > MaxTagCount)
        {
            throw new ArgumentException($"A notebook can have at most {MaxTagCount} tags.", nameof(tags));
        }

        if (list.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > MaxTagLength))
        {
            throw new ArgumentException($"Tags must be non-empty and at most {MaxTagLength} characters.", nameof(tags));
        }

        _tags = list;
        Touch();
    }

    // Access control, not a content change: deliberately no Touch(), so the access code does
    // not reorder the owner's Recently-updated list (mirrors SetFirstPage).
    // A null hash clears the access code.
    public void SetAccessCodeHash(string? accessCodeHash) => AccessCodeHash = accessCodeHash;

    // Sharing again with a different role updates the existing share in place. Sharing with the
    // owner is a business error the handler rejects up front, mirroring Page.Share.
    // Deliberately no Touch(): membership changes are access control, not content edits.
    public void Share(Guid userId, CollaboratorRole role)
    {
        var existing = _shares.FirstOrDefault(share => share.UserId == userId);
        if (existing is not null)
        {
            existing.ChangeRole(role);
            return;
        }

        _shares.Add(NotebookShare.Create(Id, userId, role));
    }

    // Idempotent: revoking a share that does not exist is a no-op.
    // Deliberately no Touch(), mirroring Share: membership changes are access control.
    public void RevokeShare(Guid userId) => _shares.RemoveAll(share => share.UserId == userId);

    public bool IsSharedWith(Guid userId) => _shares.Any(share => share.UserId == userId);

    public CollaboratorRole? SharedRoleFor(Guid userId)
        => _shares.FirstOrDefault(share => share.UserId == userId)?.Role;

    public void SoftDelete(DateTimeOffset deletedAtUtc) => DeletedAtUtc = deletedAtUtc;

    // Pointer bookkeeping only: re-heading the root chain must not mark the notebook as updated.
    public void SetFirstPage(Guid? firstPageId) => FirstPageId = firstPageId;

    public void Restore() => DeletedAtUtc = null;

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
