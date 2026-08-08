using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Notes;

public sealed class NotebookItem : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid NotebookId { get; set; }

    public Notebook Notebook { get; set; } = null!;

    public Guid? ParentId { get; set; }

    public NotebookItem? Parent { get; set; }

    public List<NotebookItem> Children { get; } = [];

    public NotebookItemType Type { get; set; }

    public required string Title { get; set; }

    public required string Slug { get; set; }

    public required string Path { get; set; }

    public int SortOrder { get; set; }

    public string? ContentFormat { get; set; }

    public string? ContentJson { get; set; }

    public string? PlainTextContent { get; set; }

    public int Revision { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset? ArchivedAtUtc { get; set; }

    public Guid? ArchivedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public void UpdateStructure(Guid? parentId, string title, string path, int? sortOrder = null)
    {
        ParentId = parentId;
        Title = title;
        Path = path;
        Slug = path.Split('/')[^1];

        if (sortOrder.HasValue)
        {
            SortOrder = sortOrder.Value;
        }
    }

    public void SetPageContent(string? contentFormat, string? contentJson, string? plainTextContent)
    {
        ContentFormat = contentFormat;
        ContentJson = contentJson;
        PlainTextContent = plainTextContent;
    }

    public void Archive(DateTimeOffset archivedAtUtc, Guid archivedByUserId)
    {
        IsArchived = true;
        ArchivedAtUtc = archivedAtUtc;
        ArchivedByUserId = archivedByUserId;
    }

    public void Restore()
    {
        IsArchived = false;
        ArchivedAtUtc = null;
        ArchivedByUserId = null;
    }
}
