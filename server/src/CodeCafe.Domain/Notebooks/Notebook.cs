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

    public void SetTags(IEnumerable<string> tags) => _tags = tags.ToList();

    // A null hash clears the access code.
    public void SetAccessCodeHash(string? accessCodeHash) => AccessCodeHash = accessCodeHash;

    // Sharing again with a different role updates the existing share in place.
    public void Share(Guid userId, CollaboratorRole role)
    {
        if (userId == OwnerId)
        {
            throw new InvalidOperationException("The owner already has full access.");
        }

        var existing = _shares.FirstOrDefault(share => share.UserId == userId);
        if (existing is not null)
        {
            existing.ChangeRole(role);
            return;
        }

        _shares.Add(NotebookShare.Create(Id, userId, role));
    }

    // Idempotent: revoking a share that does not exist is a no-op.
    public void RevokeShare(Guid userId) => _shares.RemoveAll(share => share.UserId == userId);

    public bool IsSharedWith(Guid userId) => _shares.Any(share => share.UserId == userId);

    public void SoftDelete(DateTimeOffset deletedAtUtc) => DeletedAtUtc = deletedAtUtc;

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
