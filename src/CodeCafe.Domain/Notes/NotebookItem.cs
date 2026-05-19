using CodeCafe.Domain.Common.Interfaces;

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

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
