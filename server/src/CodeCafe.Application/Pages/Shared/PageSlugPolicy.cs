using CodeCafe.Application.Common;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Shared;

// Pages derive their slug from the title, so "generate a candidate and dodge the unique index"
// is the same dance in create, import, and a title-driven slug change.
internal static class PageSlugPolicy
{
    // The best free candidate for the title, or null when the notebook has exhausted them.
    public static async Task<string?> FindAvailableAsync(
        Guid notebookId,
        string title,
        IPageRepository pages,
        CancellationToken cancellationToken
    )
    {
        var candidates = await SlugAvailability.FindAvailableAsync(
            PageSlug.GenerateFromTitle(title),
            Page.MaxSlugLength,
            1,
            async (candidate, ct) => await pages.FindBySlugAsync(notebookId, candidate, ct) is not null,
            cancellationToken
        );

        return candidates.Count > 0 ? candidates[0] : null;
    }

    // Keeps the same page and moves it to a fresh candidate; false means none is left.
    public static async Task<bool> ReKeyAsync(
        Page page,
        Guid notebookId,
        string title,
        IPageRepository pages,
        CancellationToken cancellationToken
    )
    {
        var slug = await FindAvailableAsync(notebookId, title, pages, cancellationToken);
        if (slug is null)
        {
            return false;
        }

        page.ChangeSlug(slug);
        return true;
    }
}
