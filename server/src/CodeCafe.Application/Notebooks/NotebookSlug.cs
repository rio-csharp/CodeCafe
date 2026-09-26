using CodeCafe.Application.Common;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks;

public static class NotebookSlug
{
    public static string Normalize(string slug) => Slug.Normalize(slug);

    public static string GenerateFromTitle(string title)
        => Slug.GenerateFromTitle(title, Notebook.MaxSlugLength, fallback: "notebook");

    public static bool IsValid(string slug) => Slug.IsValid(slug, Notebook.MaxSlugLength);

    public static string WithSuffix(string baseSlug, string suffix)
        => Slug.WithSuffix(baseSlug, suffix, Notebook.MaxSlugLength);
}
