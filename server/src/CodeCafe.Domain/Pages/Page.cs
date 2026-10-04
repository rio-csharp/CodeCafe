using CodeCafe.Domain.Primitives;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Domain.Pages;

public sealed class Page : Entity
{
    public const int MaxTitleLength = 200;
    public const int MaxSlugLength = 80;

    // LexoRank-style keys grow one level per tight insertion; 64 chars is far beyond practice.
    public const int MaxSortKeyLength = 64;

    private readonly List<PageShare> _shares = [];

    private Page()
        : base(Guid.Empty) { }

    private Page(Guid id, Guid notebookId, Guid? parentId, string title, string slug, string sortKey)
        : base(id)
    {
        NotebookId = notebookId;
        ParentId = parentId;
        Title = title;
        Slug = slug;
        SortKey = sortKey;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid NotebookId { get; private set; }

    // ParentId/FirstChildId/NextSiblingId are plain ids without navigation properties: soft-deleted
    // pages must keep pointing at their neighbours, and self-referencing FKs invite cascade cycles.
    public Guid? ParentId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    // Unique within the notebook, so one path segment unambiguously names one page.
    public string Slug { get; private set; } = string.Empty;

    // Dual ordering: the chains are the structural truth that writes maintain (FirstChildId heads
    // a page's child chain, NextSiblingId links siblings; the root chain hangs off the notebook),
    // while SortKey is their indexed projection that reads order by, so listing never walks a chain.
    public string SortKey { get; private set; } = string.Empty;

    public Guid? FirstChildId { get; private set; }

    public Guid? NextSiblingId { get; private set; }

    // Head of the page's top-level block chain; null for a page without blocks. Plain id,
    // mirroring the page chains.
    public Guid? FirstBlockId { get; private set; }

    public bool IsArchived { get; private set; }

    public IReadOnlyCollection<PageShare> Shares => _shares.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    // Soft-deleted pages sit in the trash until restored or purged.
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Page Create(Guid notebookId, Guid? parentId, string title, string slug, string sortKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        return new Page(Guid.CreateVersion7(), notebookId, parentId, title, slug, sortKey);
    }

    // Renaming keeps the slug: paths stay stable, like notebooks.
    public void Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Touch();
    }

    public void SetArchived(bool isArchived)
    {
        IsArchived = isArchived;
        Touch();
    }

    // Only used to recover from a slug race with a freshly generated candidate.
    public void ChangeSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Slug = slug;
        Touch();
    }

    // Reposition under a (possibly) new parent; PageChain fixes the old and new neighbours.
    // FirstChildId is deliberately untouched: the child chain moves with the page.
    public void MoveTo(Guid? parentId, string sortKey, Guid? nextSiblingId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        ParentId = parentId;
        SortKey = sortKey;
        NextSiblingId = nextSiblingId;
        Touch();
    }

    // Pointer bookkeeping only: a sibling being moved must not mark this page as updated.
    public void SetNextSibling(Guid? nextSiblingId) => NextSiblingId = nextSiblingId;

    // Pointer bookkeeping only: re-heading the child chain must not mark this page as updated.
    public void SetFirstChild(Guid? firstChildId) => FirstChildId = firstChildId;

    // Pointer bookkeeping only: re-heading the block chain must not mark this page as updated.
    // Internal because BlockChain is the single writer of the block chains.
    internal void SetFirstBlock(Guid? firstBlockId) => FirstBlockId = firstBlockId;

    // Rebalance bookkeeping only: re-keying a level must not mark untouched pages as updated.
    public void Rekey(string sortKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortKey);

        SortKey = sortKey;
    }

    // Sharing again with a different role updates the existing share in place. Sharing with the
    // notebook owner is a business error the handler rejects up front, mirroring Notebook.Share.
    // Deliberately no Touch(): a share is access control, not a content edit — the page's
    // UpdatedAtUtc is driven by block changes only.
    public void Share(Guid userId, CollaboratorRole role)
    {
        var existing = _shares.FirstOrDefault(share => share.UserId == userId);
        if (existing is not null)
        {
            existing.ChangeRole(role);
            return;
        }

        _shares.Add(PageShare.Create(Id, userId, role));
    }

    // Idempotent: revoking a share that does not exist is a no-op.
    // Deliberately no Touch(), mirroring Share.
    public void RevokeShare(Guid userId) => _shares.RemoveAll(share => share.UserId == userId);

    public bool IsSharedWith(Guid userId) => _shares.Any(share => share.UserId == userId);

    public CollaboratorRole? SharedRoleFor(Guid userId)
        => _shares.FirstOrDefault(share => share.UserId == userId)?.Role;

    public void SoftDelete(DateTimeOffset deletedAtUtc) => DeletedAtUtc = deletedAtUtc;

    public void Restore() => DeletedAtUtc = null;

    // Public so block handlers can mark the page as updated: blocks live inside the page's
    // consistency boundary, and a block mutation is page activity.
    public void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
