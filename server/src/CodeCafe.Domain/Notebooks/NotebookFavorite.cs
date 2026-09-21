namespace CodeCafe.Domain.Notebooks;

// Favorites are per-user: one row per (notebook, user) pair, like a GitHub star.
public sealed class NotebookFavorite
{
    private NotebookFavorite() { }

    public Guid NotebookId { get; private set; }

    public Guid UserId { get; private set; }

    public static NotebookFavorite Create(Guid notebookId, Guid userId)
        => new() { NotebookId = notebookId, UserId = userId };
}
