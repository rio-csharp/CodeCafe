namespace CodeCafe.Domain.Pages;

// Favorites are per-user: one row per (page, user) pair, mirroring notebook favorites.
public sealed class PageFavorite
{
    private PageFavorite() { }

    public Guid PageId { get; private set; }

    public Guid UserId { get; private set; }

    public static PageFavorite Create(Guid pageId, Guid userId)
        => new() { PageId = pageId, UserId = userId };
}
