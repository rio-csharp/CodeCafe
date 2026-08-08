using CodeCafe.Modules.Identity.Infrastructure.Identity;
using CodeCafe.Domain.Notes;
using CodeCafe.Shared.Infrastructure.Persistence;

namespace CodeCafe.Infrastructure.Tests;

/// <summary>
/// Small builders to seed the SQLite harness with the foreign-key chain the Notes
/// services expect (a user owns a notebook, a notebook owns items / favorites).
/// </summary>
internal static class NotesSeed
{
    public static ApplicationUser AddUser(this ApplicationDbContext context, Guid id, string displayName)
    {
        var user = new ApplicationUser
        {
            Id = id,
            DisplayName = displayName,
            UserName = $"{displayName}@example.com",
            NormalizedUserName = $"{displayName}@EXAMPLE.COM".ToUpperInvariant(),
            Email = $"{displayName}@example.com",
            NormalizedEmail = $"{displayName}@EXAMPLE.COM".ToUpperInvariant(),
            CreatedAtUtc = DateTimeOffset.Parse("2026-05-01T00:00:00+00:00")
        };

        context.Users.Add(user);
        return user;
    }

    public static Notebook AddNotebook(
        this ApplicationDbContext context,
        Guid id,
        Guid ownerId,
        string title,
        string slug,
        NotebookVisibility visibility,
        bool isPublished)
    {
        var notebook = new Notebook
        {
            Id = id,
            OwnerId = ownerId,
            Title = title,
            Slug = slug,
            Description = $"{title} description",
            Visibility = visibility,
            IsPublished = isPublished,
            CreatedAtUtc = DateTimeOffset.Parse("2026-05-10T00:00:00+00:00"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-05-20T00:00:00+00:00"),
            PublishedAtUtc = isPublished ? DateTimeOffset.Parse("2026-05-20T00:00:00+00:00") : null
        };

        context.Notebooks.Add(notebook);
        return notebook;
    }

    public static NotebookItem AddItem(
        this ApplicationDbContext context,
        Guid id,
        Guid notebookId,
        NotebookItemType type,
        string title,
        string path,
        int sortOrder,
        Guid? parentId = null,
        string? plainTextContent = null,
        bool isArchived = false)
    {
        var item = new NotebookItem
        {
            Id = id,
            NotebookId = notebookId,
            ParentId = parentId,
            Type = type,
            Title = title,
            Slug = path.Split('/')[^1],
            Path = path,
            SortOrder = sortOrder,
            ContentFormat = type == NotebookItemType.Page ? "tiptap_json" : null,
            PlainTextContent = plainTextContent,
            IsArchived = isArchived,
            ArchivedAtUtc = isArchived ? DateTimeOffset.Parse("2026-05-25T00:00:00+00:00") : null,
            CreatedAtUtc = DateTimeOffset.Parse("2026-05-15T00:00:00+00:00"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-05-18T00:00:00+00:00"),
            Revision = 1
        };

        context.NotebookItems.Add(item);
        return item;
    }

    public static NotebookFavorite AddFavorite(this ApplicationDbContext context, Guid notebookId, Guid userId)
    {
        var favorite = new NotebookFavorite
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            UserId = userId,
            CreatedAtUtc = DateTimeOffset.Parse("2026-05-22T00:00:00+00:00")
        };

        context.NotebookFavorites.Add(favorite);
        return favorite;
    }
}
