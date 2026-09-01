using CodeCafe.Domain.Common;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.Services;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Notes;

public sealed class NotebookItem : Entity, IAuditableEntity
{
    private NotebookItem() { }

    private NotebookItem(
        Guid id,
        Guid notebookId,
        Guid? parentId,
        NotebookItemType type,
        string title,
        NotebookSlug slug,
        NotebookPath path,
        int sortOrder,
        DateTimeOffset createdAtUtc
    )
    {
        Id = id;
        NotebookId = notebookId;
        ParentId = parentId;
        Type = type;
        Title = title;
        Slug = slug;
        Path = path;
        SortOrder = sortOrder;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid NotebookId { get; private set; }

    public Guid? ParentId { get; private set; }

    public NotebookItemType Type { get; private set; }

    public string Title { get; private set; } = null!;

    public NotebookSlug Slug { get; private set; } = null!;

    public NotebookPath Path { get; private set; } = null!;

    public int SortOrder { get; private set; }

    public string? ContentJson { get; private set; }

    public string? PlainTextContent { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public Guid? ArchivedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    internal static NotebookItem Create(
        Guid id,
        Guid notebookId,
        Guid? parentId,
        NotebookItemType type,
        string title,
        NotebookSlug slug,
        NotebookPath path,
        int sortOrder,
        DateTimeOffset createdAtUtc
    ) => new(id, notebookId, parentId, type, title, slug, path, sortOrder, createdAtUtc);

    internal void Rename(string title) => Title = title;

    internal void UpdateStructure(Guid? parentId, string title, string path, int? sortOrder = null)
    {
        ParentId = parentId;
        Title = title;
        Path = NotebookPath.Create(path);
        Slug = NotebookSlug.Create(path.Split('/')[^1]);

        if (sortOrder.HasValue)
        {
            SortOrder = sortOrder.Value;
        }
    }

    internal void SetPageContent(string? contentJson)
    {
        ContentJson = contentJson;
        PlainTextContent = TipTapPlainTextExtractor.Extract(contentJson);
    }

    internal void UpdatePath(string path)
    {
        Path = NotebookPath.Create(path);
        Slug = NotebookSlug.Create(path.Split('/')[^1]);
    }

    internal void Archive(DateTimeOffset archivedAtUtc, Guid archivedByUserId)
    {
        IsArchived = true;
        ArchivedAtUtc = archivedAtUtc;
        ArchivedByUserId = archivedByUserId;
    }

    internal void Restore()
    {
        IsArchived = false;
        ArchivedAtUtc = null;
        ArchivedByUserId = null;
    }
}
