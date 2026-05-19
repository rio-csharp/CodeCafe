using CodeCafe.Domain.Common.Interfaces;

namespace CodeCafe.Domain.Notes;

public sealed class NotebookFavorite : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid NotebookId { get; set; }

    public Notebook Notebook { get; set; } = null!;

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
