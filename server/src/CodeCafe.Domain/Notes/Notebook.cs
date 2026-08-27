using CodeCafe.Domain.Common;

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

    public void Rename(string title)
    {
        Title = title;
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void ApplyVisibility(NotebookVisibility visibility, DateTimeOffset now)
    {
        var wasPublished = IsPublished;

        Visibility = visibility;
        IsPublished = visibility == NotebookVisibility.Public;

        if (!IsPublished)
        {
            PublishedAtUtc = null;
            return;
        }

        PublishedAtUtc = wasPublished ? PublishedAtUtc ?? now : now;
    }
}
