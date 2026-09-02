using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes.ValueObjects;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainSlugGenerator = CodeCafe.Domain.Notes.Services.NotebookSlugGenerator;

namespace CodeCafe.Infrastructure.Notes.Write;

public sealed class NotebookSlugGenerator(ApplicationDbContext dbContext) : INotebookSlugGenerator
{
    // Reserve room so a "-99" suffix still fits the slug length cap.
    private const int SuffixBudget = 4;
    private const int MaxAttempts = 99;

    public async Task<NotebookSlug> GenerateUniqueSlugAsync(
        string title,
        Guid? excludeNotebookId,
        CancellationToken cancellationToken
    )
    {
        var baseSlug = DomainSlugGenerator.FromTitle(
            title,
            "notebook",
            NotebookSlug.MaxLength - SuffixBudget
        );
        var candidate = baseSlug;

        for (var attempt = 2; ; attempt++)
        {
            var slug = NotebookSlug.Create(candidate);
            if (await IsSlugAvailableAsync(slug, excludeNotebookId, cancellationToken))
            {
                return slug;
            }
            if (attempt >= MaxAttempts)
            {
                throw new InvalidOperationException($"Could not generate a unique slug for '{title}'.");
            }
            candidate = $"{baseSlug}-{attempt}";
        }
    }

    public async Task<bool> IsSlugAvailableAsync(
        NotebookSlug slug,
        Guid? excludeNotebookId,
        CancellationToken cancellationToken
    )
    {
        var taken = await dbContext.Notebooks.AnyAsync(
            notebook =>
                notebook.Slug == slug
                && (excludeNotebookId == null || notebook.Id != excludeNotebookId),
            cancellationToken
        );
        return !taken;
    }
}
