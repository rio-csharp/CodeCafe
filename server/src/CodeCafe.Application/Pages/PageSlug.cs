using CodeCafe.Application.Common;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages;

public static class PageSlug
{
    public static string Normalize(string slug) => Slug.Normalize(slug);

    public static string GenerateFromTitle(string title)
        => Slug.GenerateFromTitle(title, Page.MaxSlugLength, fallback: "page");

    public static bool IsValid(string slug) => Slug.IsValid(slug, Page.MaxSlugLength);
}
