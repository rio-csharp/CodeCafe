using CodeCafe.Domain.Common;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.Events;
using CodeCafe.Domain.Notes.Services;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Notes;

public sealed class Notebook : Entity, IAuditableEntity
{
    private readonly List<NotebookItem> _items = [];

    private Notebook() { }

    private Notebook(
        Guid id,
        Guid ownerId,
        string title,
        NotebookSlug slug,
        string? description,
        NotebookVisibility visibility,
        DateTimeOffset createdAtUtc
    )
    {
        Id = id;
        OwnerId = ownerId;
        Title = title;
        Slug = slug;
        Description = description;
        Visibility = visibility;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = null!;

    public NotebookSlug Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    public NotebookVisibility Visibility { get; private set; } = NotebookVisibility.Private;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public string? AccessCodeHash { get; private set; }

    public IReadOnlyCollection<NotebookItem> Items => _items.AsReadOnly();

    public static Notebook Create(
        Guid id,
        Guid ownerId,
        string title,
        NotebookSlug slug,
        string? description,
        NotebookVisibility visibility,
        DateTimeOffset createdAtUtc
    )
    {
        var notebook = new Notebook(id, ownerId, title, slug, description, visibility, createdAtUtc)
        {
            // Creating directly as Public is not a visibility change, so no event is raised,
            // but the publish timestamp must still be set.
            PublishedAtUtc = visibility == NotebookVisibility.Public ? createdAtUtc : null,
        };
        return notebook;
    }

    public void Rename(string title) => Title = title;

    public void ChangeSlug(NotebookSlug slug) => Slug = slug;

    public void SetDescription(string? description) => Description = description;

    public void ApplyVisibility(NotebookVisibility visibility, DateTimeOffset now)
    {
        if (visibility == Visibility)
        {
            return;
        }

        var previous = Visibility;
        Visibility = visibility;

        PublishedAtUtc = visibility == NotebookVisibility.Public ? now : null;
        if (visibility != NotebookVisibility.Unlisted)
        {
            AccessCodeHash = null;
        }

        RaiseDomainEvent(new NotebookVisibilityChangedDomainEvent(Id, previous, visibility, now));
    }

    public NotebookAccessCodeViolation? SetAccessCode(string accessCodeHash)
    {
        if (Visibility != NotebookVisibility.Unlisted)
        {
            return NotebookAccessCodeViolation.NotUnlisted;
        }

        AccessCodeHash = accessCodeHash;
        return null;
    }

    public void ClearAccessCode() => AccessCodeHash = null;

    public NotebookItemAddViolation? AddItem(
        Guid itemId,
        NotebookItemType type,
        string title,
        NotebookSlug? slug,
        Guid? parentId,
        int sortOrder,
        DateTimeOffset now
    )
    {
        var parent = parentId is null ? null : _items.SingleOrDefault(existingItem => existingItem.Id == parentId);
        if (parentId is not null)
        {
            if (parent is null)
            {
                return NotebookItemAddViolation.ParentNotFound;
            }

            if (parent.Type != NotebookItemType.Folder)
            {
                return NotebookItemAddViolation.ParentNotFolder;
            }
        }

        var parentPath = parent?.Path.Value;
        string path;
        if (slug is not null)
        {
            if (slug.Value.Length > NotebookPath.GetSlugBudget(parentPath))
            {
                return NotebookItemAddViolation.NoRoomForChild;
            }

            path = string.IsNullOrEmpty(parentPath) ? slug.Value : $"{parentPath}/{slug.Value}";
            if (_items.Any(existingItem => existingItem.Path.Value == path))
            {
                return NotebookItemAddViolation.SlugConflict;
            }
        }
        else
        {
            if (!NotebookPath.HasRoomForChild(parentPath))
            {
                return NotebookItemAddViolation.NoRoomForChild;
            }

            path = NotebookItemTree.GeneratePath(_items, parentPath, title, itemId);
        }

        _items.Add(
            NotebookItem.Create(
                itemId,
                Id,
                parentId,
                type,
                title,
                NotebookSlug.Create(path.Split('/')[^1]),
                NotebookPath.Create(path),
                sortOrder,
                now
            )
        );
        return null;
    }

    public NotebookItemRenameViolation? RenameItem(Guid itemId, string title)
    {
        var item = _items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotebookItemRenameViolation.NotFound;
        }

        item.Rename(title);
        return null;
    }

    public NotebookItemArchiveViolation? ArchiveItem(Guid itemId, Guid actorId, DateTimeOffset now)
    {
        var item = _items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotebookItemArchiveViolation.NotFound;
        }

        if (item.IsArchived)
        {
            return NotebookItemArchiveViolation.AlreadyArchived;
        }

        var idsToArchive = NotebookItemTree.GetDescendantIds(_items, itemId);
        idsToArchive.Add(itemId);
        foreach (var entry in _items.Where(existingItem => idsToArchive.Contains(existingItem.Id)))
        {
            entry.Archive(now, actorId);
        }

        RaiseDomainEvent(new NotebookItemArchivedDomainEvent(Id, itemId, actorId, now));
        return null;
    }

    public NotebookItemRestoreViolation? RestoreItem(Guid itemId, DateTimeOffset now)
    {
        var item = _items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotebookItemRestoreViolation.NotFound;
        }

        var violation = NotebookItemTree.ValidateRestore(_items, item);
        if (violation is not null)
        {
            return violation;
        }

        var idsToRestore = NotebookItemTree.GetDescendantIds(_items, itemId);
        idsToRestore.Add(itemId);
        foreach (var entry in _items.Where(existingItem => idsToRestore.Contains(existingItem.Id)))
        {
            entry.Restore();
        }

        RaiseDomainEvent(new NotebookItemRestoredDomainEvent(Id, itemId, now));
        return null;
    }
}
