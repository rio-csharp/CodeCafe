using System.Linq.Expressions;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Read;

internal static class NotebookAccessPredicates
{
    // includeUnlisted: surfaces built on explicit prior knowledge (favorites) may list unlisted
    // notebooks; discovery surfaces (search) must never leak them.
    public static Expression<Func<Notebook, bool>> ReadableByUser(
        ApplicationDbContext dbContext,
        Guid userId,
        bool includeUnlisted
    )
    {
        return notebook =>
            notebook.OwnerId == userId
            || notebook.Visibility == NotebookVisibility.Public
            || (includeUnlisted && notebook.Visibility == NotebookVisibility.Unlisted)
            || (userId != Guid.Empty
                && dbContext.NotebookShares.Any(share =>
                    share.NotebookId == notebook.Id && share.UserId == userId));
    }
}
