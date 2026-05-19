using CodeCafe.Domain.Common.Interfaces;

namespace CodeCafe.Domain.Notes;

public sealed class Notebook : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public required string Title { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public NotebookVisibility Visibility { get; set; } = NotebookVisibility.Private;

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public List<NotebookItem> Items { get; } = [];

    public List<NotebookFavorite> Favorites { get; } = [];
}
